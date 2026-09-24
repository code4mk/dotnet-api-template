# Docker and deployment

## Files

| File | Purpose |
| --- | --- |
| `docker-compose.yml` (root) | Every local setup, selected with profiles |
| `docker/Dockerfile` | Production image: multi-stage build, runtime-only image, non-root user |
| `docker/Dockerfile.dev` | Development image: SDK + `dotnet watch` on the mounted source |
| `.dockerignore` | Keeps `bin/`, `obj/`, tests, docs and every `.env*` file out of images |

Run Compose commands from the repository root; it reads `.env` there automatically.

## Docker Compose profiles

| Command | Starts |
| --- | --- |
| `docker compose up -d db mailpit` | PostgreSQL + Mailpit, for the API running on your machine with `dotnet watch` |
| `docker compose --profile dev up --build` | + `api-dev`: the API in Docker with auto reload |
| `docker compose --profile prod up -d --build` | + `api`: the production image |
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

`api-dev` runs `dotnet watch` with the source mounted: save a file under `src/` and the API reloads, no
image rebuild. The container keeps its own `bin/` and `obj/`. Rebuild (`--build`) only after changing
`docker/Dockerfile.dev` or packages.

### `prod` profile

`api` is the real production image, so code changes need `--build`. Use it to check the image locally
before deploying. It sends email to `EMAIL_HOST` from `.env` (not Mailpit).

## The production image

`docker/Dockerfile`:

1. **Build stage** (SDK): restores packages first (cached until a `.csproj` or `Directory.*.props` changes),
   then publishes in Release.
2. **Runtime stage** (`aspnet:10.0`): only the published output, running as the built-in non-root user,
   listening on port 8080.

```bash
docker build -f docker/Dockerfile -t dotnetapitemplate-slug-api .
docker run --rm -p 8080:8080 --env-file .env -e DB_HOST=host.docker.internal dotnetapitemplate-slug-api
```

No `.env` file is inside the image: configuration comes from the environment at runtime.

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
   `CORS_ALLOWED_ORIGINS` with the real frontend URL(s).
2. **Database migrations** before the new version starts (the API never migrates itself), with a reviewed
   script or a migration bundle, see [Database and migrations](database-and-migrations.md#production).
3. **Start the container** from the new image; expose port 8080 behind HTTPS (reverse proxy, load balancer
   or platform ingress). The container itself speaks plain HTTP.
4. **Health check:** point the platform at `GET /health`.
5. **Smoke test:** `GET /` returns the name, environment and version.

Other notes:

- Swagger and `/openapi` are off outside Development.
- Logs go to stdout; collect them with your platform (see [Logging](logging-and-correlation-ids.md)).
- The app is stateless: run several instances behind a load balancer. Tokens are validated with the
  shared `JWT_SIGNING_KEY`, so every instance needs the same key.
- Behind a reverse proxy, forward the original scheme/host if the app ever needs them
  (`app.UseForwardedHeaders()`).
