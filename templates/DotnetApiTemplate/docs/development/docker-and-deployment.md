# Docker and deployment

## Files

| File | Purpose |
| --- | --- |
| `docker-compose.yml` (root) | Every local setup, selected with profiles |
| `docker/Dockerfile` | Production image: API + background job worker under supervisor, non-root user |
| `docker/supervisor/` | `supervisord.conf` (the two processes) and `exit-on-fatal.py` (stops the container if one can't stay up) |
| `docker/Dockerfile.dev` | Development image: SDK + `dotnet watch` on the mounted source |
| `deploy/supervisor/dotnetapitemplate.conf` | Supervisor programs for a VM / bare-metal server |
| `.dockerignore` | Keeps `bin/`, `obj/`, tests, docs and every `.env*` file out of images |

Run Compose commands from the repository root; it reads `.env` there automatically.

## Docker Compose profiles

| Command | Starts |
| --- | --- |
| `docker compose up -d db mailpit` | PostgreSQL + Mailpit, for the API running on your machine with `dotnet watch` |
| `docker compose --profile dev up --build` | + `api-dev`: the API in Docker with auto reload |
| `docker compose --profile prod up -d --build` | + `api`: the production image (API + worker) |
| `docker compose ps` / `logs -f api-dev` | status / follow logs |
| `docker compose --profile dev --profile prod down` | stop everything (`-v` also deletes the database and NuGet cache volumes) |

| Service | Profile | Port on your machine |
| --- | --- | --- |
| `db` (PostgreSQL 17) | always | `127.0.0.1:${DB_PORT}` (`54320` in this project) |
| `mailpit` | `dev` | SMTP `${EMAIL_PORT}`, inbox `${MAILPIT_UI_PORT}` (`21025`, `28025`) |
| `api-dev` | `dev` | `${API_PORT}` (`18080`) |
| `api` | `prod` | `${API_PORT}` (`18080`) |

The ports were picked when the project was created, so several projects can run side by side. PostgreSQL
and Mailpit listen on `127.0.0.1` only. Inside Docker, the API always reaches the database at `db:5432`
and (in `dev`) Mailpit at `mailpit:1025`, whatever `.env` says for your machine.

Migrations still run manually, from your machine against the published port:
`dotnet ef database update --project src/DotnetApiTemplate.Api`.

### `dev` profile

`api-dev` runs `dotnet watch` with the source mounted (`APP_ROLE=all`: API and background jobs in one
process): save a file under `src/` and the API reloads, no image rebuild. The container keeps its own `bin/` and `obj/`. Rebuild (`--build`) only after changing
`docker/Dockerfile.dev` or packages.

### `prod` profile

`api` is the real production image, so code changes need `--build`. Use it to check the image locally
before deploying. It sends email to `EMAIL_HOST` from `.env`: `localhost` there is the container itself, so
for local testing set `EMAIL_HOST=mailpit` and `EMAIL_PORT=1025`, or failed emails wait in the job retries.

`docker compose stop` gives it 90s (`stop_grace_period`), so running jobs can finish.

## The production image

`docker/Dockerfile`:

1. **Build stage** (SDK): restores packages first (cached until a `.csproj` or `Directory.*.props` changes),
   then publishes in Release.
2. **Runtime stage** (`aspnet:10.0` + supervisor): the published output, running as the built-in non-root
   user. **supervisord** starts two processes from the same build:

| Process | `APP_ROLE` | Port | Does |
| --- | --- | --- | --- |
| `api` | `api` | 8080 (publish this) | HTTP API, `/jobs` dashboard, enqueues jobs |
| `worker` | `worker` | 8081 (internal) | runs background jobs and recurring schedules; only `/health` |

```bash
docker build -f docker/Dockerfile -t dotnetapitemplate-slug-api .
docker run --rm -p 8080:8080 --env-file .env -e DB_HOST=host.docker.internal dotnetapitemplate-slug-api
```

No `.env` file is inside the image: configuration comes from the environment at runtime (supervisor
passes it to both processes and sets `APP_ROLE` / port per process).

How the container behaves:

| Situation | What happens |
| --- | --- |
| A process crashes | supervisor restarts it (`autorestart`) |
| A process can't stay up (e.g. missing migrations: 3 quick failures) | `exit-on-fatal.py` stops supervisord, the container exits, and the platform restarts it: the crash loop is visible instead of a half-working container |
| Health check | `HEALTHCHECK` passes only when **both** `8080/health` and `8081/health` answer |
| `docker stop` / platform stop (SIGTERM) | supervisor stops both gracefully: requests finish (30s), running jobs finish or are re-queued (75s). Give the container at least 90s to stop |
| Status / restart one process | `docker exec <container> supervisorctl status`, `supervisorctl restart worker` |
| Logs | both processes write to the container's stdout; each startup logs its role |

**Separate containers instead** (scale API and workers independently): run the same image twice without
supervisor, e.g. `--entrypoint dotnet ... DotnetApiTemplate.Api.dll` with `APP_ROLE=api` (N copies) and
`APP_ROLE=worker` (M copies). No code change.

## CI/CD (GitHub Actions)

| Workflow | Trigger | Does |
| --- | --- | --- |
| `.github/workflows/build-and-test.yml` | push / pull request to `main` | build, run all tests with coverage |
| `.github/workflows/deploy.yml` | tag `v*.*.*` | build the production image and push it to GitHub Container Registry (`ghcr.io/<owner>/<repo>/api`), tagged with the version and `latest` |

Release:

```bash
git tag v1.2.0
git push origin v1.2.0
```

`deploy.yml` ends at pushing the image: add the step for your platform (Azure Web App, Kubernetes, a VM
over SSH, ...).

## Deploying

Checklist for every environment:

1. **Configuration** in the platform's environment or secret store (or a `.env` on the server), see
   [Configuration](configuration-and-environments.md):
   `APP_ENV=prod`, `DB_*`, a new `JWT_SIGNING_KEY` (`openssl rand -base64 48`), `EMAIL_*`,
   `CORS_ALLOWED_ORIGINS` with the real frontend URL(s), `JOBS_DASHBOARD_USERNAME` / `_PASSWORD` if you want
   the dashboard. Leave `APP_ROLE` unset: the image sets it per process.
2. **Database migrations** before the new version starts (the API never migrates itself), with a reviewed
   script or a migration bundle, see [Database and migrations](database-and-migrations.md#production).
   This also creates or upgrades Hangfire's `hangfire` schema.
3. **Start the container** from the new image; expose port 8080 behind HTTPS (reverse proxy, load balancer
   or platform ingress). The container itself speaks plain HTTP. Allow at least 90s for it to stop.
4. **Health check:** the image's `HEALTHCHECK`, or point the platform at `GET /health` (API) — the worker's
   `/health` is on 8081 inside the container.
5. **Smoke test:** `GET /` returns the name, environment and version; `/jobs` (with the dashboard login) lists
   the worker under **Servers**.

Other notes:

- Swagger and `/openapi` are off outside Development.
- Logs go to stdout; collect them with your platform (see [Logging](logging-and-correlation-ids.md)).
- The app is stateless: run several containers behind a load balancer. Tokens are validated with the
  shared `JWT_SIGNING_KEY`, so every instance needs the same key. Every container's worker joins the same
  job queue; each job still runs once. Mind the database connections: containers × 2 processes ×
  `DB_MAX_POOL_SIZE` (see [Background jobs](background-jobs.md#scaling)).

## Deploying to a VM with supervisor

`deploy/supervisor/dotnetapitemplate.conf` runs the same two processes directly on a server:

```bash
dotnet publish src/DotnetApiTemplate.Api -c Release -o /opt/dotnetapitemplate   # or copy the build output
sudo useradd --system dotnetapitemplate && sudo mkdir -p /var/log/dotnetapitemplate
sudo chown dotnetapitemplate /var/log/dotnetapitemplate
# settings: /opt/dotnetapitemplate/.env (APP_ENV=prod, DB_*, JWT_SIGNING_KEY, EMAIL_*, JOBS_*), mode 600
sudo cp deploy/supervisor/dotnetapitemplate.conf /etc/supervisor/conf.d/
sudo supervisorctl reread && sudo supervisorctl update
sudo supervisorctl status                        # dotnetapitemplate:dotnetapitemplate-api / -worker RUNNING
```

Deploy a new version: apply migrations, replace the files, `sudo supervisorctl restart dotnetapitemplate:*`.
Put nginx/Caddy in front of port 8080 for HTTPS.
- Behind a reverse proxy, forward the original scheme/host if the app ever needs them
  (`app.UseForwardedHeaders()`).
