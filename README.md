# .NET Minimal API Template

A `dotnet new` template that creates a ready-to-run ASP.NET Core Minimal API solution with our company
standards built in. Every new backend starts from here, so all projects share the same structure,
conventions and tooling.

```bash
dotnet new dotnet-api-template -n NexusRE -o nexusre-backend
```

## What you get

| Area | Included |
| --- | --- |
| API | .NET 10 Minimal APIs, route groups, `TypedResults`, OpenAPI with Swagger UI (JWT **Authorize**) in Development |
| Structure | Feature folders with a service layer (`Endpoints`, `IService`, `Service`, `Dtos`, `Mappings`); manual or automatic feature registration (`--auto-discovery`) |
| Errors | `Result<T>` pattern, RFC 7807 ProblemDetails, global exception handler |
| Validation | .NET 10 built-in validation with data annotations on request DTOs |
| Data | EF Core with PostgreSQL, entity configurations, `InitialCreate` migration (applied manually, never at startup) |
| Security | JWT bearer authentication, Admin policy, password hashing, CORS for exact origins from `.env` |
| Email | Typed emails with Scriban templates, shared layout, CSS inlining (PreMailer.Net), MailKit SMTP, Mailpit inbox for local development |
| Background jobs | Hangfire + PostgreSQL: queued, delayed and recurring jobs with retries, `/jobs` dashboard (basic auth), API and worker in one container under supervisor |
| Operations | Correlation id middleware, `/health` endpoint, structured logging |
| Samples | Auth (login), Users, Products features, `.http` request file |
| Tests | Unit tests (EF Core in-memory) and integration tests (`WebApplicationFactory`) |
| DevOps | Multi-stage Dockerfile, Docker Compose (with a `dotnet watch` auto-reload dev setup), GitHub Actions for CI and image publishing |
| Docs | ADRs, architecture overview, API conventions, getting started, coding guidelines |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL and containers)

Check your SDK with `dotnet --version`.

## 1. Install the template

Install it once per machine. Choose one option.

**Option A: from nuget.org (recommended)**

```bash
dotnet new install Code4mk.MinimalApi.Template
```

**Option B: from source (for template maintainers)**

```bash
git clone https://github.com/code4mk/dotnet-api-template.git
cd dotnet-api-template
dotnet new install ./templates/DotnetApiTemplate
```

Confirm it is installed:

```bash
dotnet new list dotnet-api-template
```

## 2. Create a new project

Go to the folder where you keep your projects (**not** inside this template repository), then run:

```bash
cd D:\Projects            # or ~/projects
dotnet new dotnet-api-template -n NexusRE -o nexusre-backend
```

- `-n` is the **.NET name**: solution, projects and namespaces. Use PascalCase, for example `NexusRE` or `Acme.Billing`.
- `-o` is the **folder / Git repo name**. Use lowercase with dashes, for example `nexusre-backend`.

Do not put dashes in `-n`. `-n nexusre-backend` produces namespaces like `nexusre_backend.Api`.

Options:

| Option | Default | Effect |
| --- | --- | --- |
| `--auto-discovery true` | `false` | Feature endpoints (`IEndpoints` classes) and services (`XService : IXService`) are found automatically, in any folder under `Features/`. With `false` you add one line per feature in `AddFeatures()` and `MapFeatures()`. Can be switched later in `Common/Features/FeatureDiscovery.cs`. |

The result:

```text
nexusre-backend/
├── NexusRE.sln
├── src/NexusRE.Api/                   namespace NexusRE.Api
├── tests/NexusRE.UnitTests/
├── tests/NexusRE.IntegrationTests/
├── docker-compose.yml                 Compose project "nexusre", image "nexusre-api"
├── docker/                            Dockerfile (production), Dockerfile.dev (auto reload)
├── .env.example                       copy to .env
├── docs/
├── scripts/
└── .github/workflows/
```

Every `DotnetApiTemplate` in file names, folders, namespaces, the solution, Docker files and docs is replaced
automatically. Project GUIDs and the user secrets id are regenerated.

## 3. First steps in the new project

```bash
cd nexusre-backend

# Build and test
dotnet build
dotnet test

# Create your settings, then start PostgreSQL and Mailpit
cp .env.example .env          # Windows PowerShell: Copy-Item .env.example .env
docker compose up -d db mailpit

# Create the tables (the API never changes the database schema on its own)
dotnet tool restore
dotnet ef database update --project src/NexusRE.Api

# Run the API (reloads when you save a file)
dotnet watch --project src/NexusRE.Api
```

Open Swagger UI at `http://localhost:5080/swagger`, or use the sample requests in `src/NexusRE.Api/NexusRE.Api.http`.
The database starts empty: register a user with `POST /api/users`, then log in with `POST /api/auth/login`.

Then:

1. **Create the Git repo:** `git init`, commit, and push to a new GitHub repo named like the folder (`nexusre-backend`).
2. **Change the model with migrations:** after changing entities, run
   `dotnet ef migrations add <Name> --project src/NexusRE.Api --output-dir Data/Migrations`, review the
   generated code, then `dotnet ef database update --project src/NexusRE.Api`.
3. **Set real secrets** for anything beyond your machine (see below).
4. **Remove sample features** you don't need, or copy `Features/Products` to start a new feature.
5. **Update the project README** with what the service does.

Developer guides inside every project: `docs/README.md` (getting started, configuration, database and
migrations, errors and exceptions, auth, email, CORS, JSON, Swagger, logging, testing, Docker and deployment).

## Hidden files

Several important files start with a dot and are **hidden by default** in Finder and sometimes in
Windows Explorer: `.env.example` (and your `.env`), `.gitignore`, `.editorconfig`, `.github/`, `.config/`,
`.dockerignore` and `.template.config/`. They are there.

- macOS Finder: press `Cmd + Shift + .`
- Windows Explorer: View → Show → Hidden items
- Terminal: `ls -a` (macOS/Linux) or `dir /a` (Windows)

## Secrets and configuration

Database, JWT and email settings live in dotenv files at the project root. The API and Docker Compose
read only `.env`. A new project contains only `.env.example`, the one env file that is committed
(`.gitignore` ignores `.env` and every `.env.*` except it); create `.env` from it before the first run.

| File | Committed | Contents |
| --- | --- | --- |
| `.env.example` | yes | every variable with local defaults; start with `cp .env.example .env` |
| `.env` | no | active settings |
| `.env.dev`, `.env.prod`, ... | no | optional presets you create from `.env.example` and copy onto `.env` |

| Variable | Purpose |
| --- | --- |
| `APP_ENV` | `dev`, `stage` or `prod` → ASP.NET Core Development, Staging, Production |
| `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | PostgreSQL connection; `DB_PORT` is also the host port Docker publishes |
| `API_PORT` | host port of the API container |
| `CORS_ALLOWED_ORIGINS` | browser frontends allowed to call the API from other origins (comma-separated, exact origins; empty = none) |
| `JWT_SIGNING_KEY` | JWT signing key, 32+ characters |

On a server: create `.env` from `.env.example` with `APP_ENV=prod` and real values (never the
development defaults), or set the same variables in the platform's environment or secret store;
real environment variables always win over `.env`.
Email uses `EMAIL_HOST`, `EMAIL_USERNAME`, ... in the same files. In development it goes to Mailpit
(inbox at `http://localhost:<MAILPIT_UI_PORT>`); an empty `EMAIL_HOST` means emails are only logged.
In code, settings are typed classes bound from these variables, like pydantic `BaseSettings`, and
validated at startup; see `docs/development/getting-started.md` in a generated project.

## Using a different database

The template uses PostgreSQL. To switch to SQL Server:

1. In `Directory.Packages.props` and `src/*.Api/*.Api.csproj`, replace `Npgsql.EntityFrameworkCore.PostgreSQL` with `Microsoft.EntityFrameworkCore.SqlServer`.
2. In `Common/Extensions/ServiceCollectionExtensions.cs`, replace `UseNpgsql` with `UseSqlServer`.
3. Update `DatabaseSettings` (connection string) and the `db` service in `docker-compose.yml`.

## Updating and removing the template

```bash
# Check for and install newer versions (nuget.org)
dotnet new update

# Reinstall after pulling changes (source)
dotnet new install ./templates/DotnetApiTemplate --force

# Uninstall
dotnet new uninstall Code4mk.MinimalApi.Template   # package
dotnet new uninstall ./templates/DotnetApiTemplate                 # source
```

Updating the template only affects **new** projects. Existing projects are never changed.

## Repository layout

```text
dotnet-api-template/
├── README.md                     this guide
├── CHANGELOG.md                  template version history
├── TemplatePack.csproj      packs the template into a NuGet package
├── .github/workflows/
│   ├── template-ci.yml           creates a sample project, builds and tests it (manual run)
│   └── publish.yml               publishes the package to nuget.org (manual run)
└── templates/
    └── DotnetApiTemplate/        the template itself (everything a new project gets)
        ├── .template.config/template.json
        ├── DotnetApiTemplate.sln
        ├── src/ tests/ docker/ docs/ scripts/ .github/
        └── ...
```

The `.github/workflows` inside `templates/DotnetApiTemplate` belong to generated projects. They do not run in this repository.

## Maintaining the template

Rules:

- Keep `DotnetApiTemplate` as the placeholder name everywhere. Never rename it.
- Keep `dotnetapitemplate-slug` where a lowercase, dash-only name is needed (Docker, Compose).
- Set package versions only in `templates/DotnetApiTemplate/Directory.Packages.props`.
- Never create projects inside this repository.

Workflow for a change:

1. Create a branch and edit files under `templates/DotnetApiTemplate`.
2. Test locally:

   ```bash
   dotnet new install ./templates/DotnetApiTemplate --force
   cd ..
   dotnet new dotnet-api-template -n Test.Project -o test-project
   cd test-project && dotnet build && dotnet test
   cd .. && rm -rf test-project
   ```

3. Add an entry to `CHANGELOG.md`.
4. Open a pull request. Optionally run `template-ci` from the Actions tab.
5. After merging, publish the new version (see below).

## Publishing to nuget.org

Packages are published under the nuget.org account **Code4mk**.

**From GitHub Actions:** add a nuget.org API key as the repository secret `NUGET_API_KEY`
(Settings → Secrets and variables → Actions). Then open Actions → `publish` → **Run workflow** and enter the version (e.g. `1.1.0`).

The `publish` workflow packs the template with that version and pushes it to nuget.org.
It does not run automatically; uncomment the `push` trigger in `publish.yml` to publish on version tags again.

**Manually from your machine:**

```bash
dotnet pack TemplatePack.csproj -c Release -o artifacts -p:PackageVersion=1.0.0

# macOS / Linux
export NUGET_API_KEY="your-key"
dotnet nuget push artifacts/Code4mk.MinimalApi.Template.1.0.0.nupkg \
  --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY

# Windows PowerShell
$env:NUGET_API_KEY = "your-key"
dotnet nuget push artifacts\Code4mk.MinimalApi.Template.1.0.0.nupkg `
  --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
```

Create the API key on nuget.org (username → API Keys) with the **Push** scope and glob pattern `Code4mk.*`.
New versions take 15 to 30 minutes to appear after validation. Every publish needs a new version number;
a broken version can be unlisted on nuget.org but never deleted.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `No templates found matching: 'dotnet-api-template'` | Install the template (step 1) and check `dotnet new list dotnet-api-template`. |
| Can't find `.env` | It's a hidden file; see [Hidden files](#hidden-files). If it's missing (fresh clone), `cp .env.example .env`. |
| Settings validation error on startup (e.g. `JWT_SIGNING_KEY: ... minimum length of '32'`) | Fix the named variable in `.env`; every invalid variable is listed. |
| API can't connect to the database | Start it with `docker compose up -d db` from the project root, and check `DB_HOST`/`DB_PORT` in `.env`. |
| `required variable DB_NAME is missing a value` | Run Docker Compose from the project root (where `docker-compose.yml` and `.env` are), and create `.env` with `cp .env.example .env`. |
| `port is already allocated` | Another container uses that port. Pick a free `DB_PORT` or `API_PORT` in `.env`. |
| Namespaces like `my_app.Api` | You used dashes in `-n`. Use PascalCase in `-n` and dashes only in `-o`. |
| New project contains another project inside it | You ran `dotnet new` inside the template repo. Delete it and run from another folder. |
