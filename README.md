# Code4mk ASP.NET Core API Template

**Start every .NET API production-ready.** One command creates an ASP.NET Core Minimal API solution (.NET 10)
with the parts every real backend needs already built, tested and documented. You write features from the first hour.

```bash
dotnet new install Code4mk.MinimalApi.Template
dotnet new dotnet-api-template -n Acme.Billing -o acme-billing
```

## Why this template

- **Ready for production:** structured errors, validated settings, health checks, correlation ids and a production
  Docker image are part of every new project.
- **Fails fast:** missing or invalid environment variables, broken dependency injection and wrong CORS origins stop
  the app at startup with a clear message. You find out before your users do.
- **Safe by default:**
  - strict JSON parsing;
  - response records, so internal fields can't leak;
  - exact CORS origins;
  - no automatic database changes at startup;
  - locked package versions.
- **Consistent across projects:** every service built from it has the same structure, error format and conventions.
  Moving between projects feels like home.
- **Documented:** each generated project ships with a developer guide per topic, plus ADRs explaining the big
  decisions.
- **Pleasant to develop:**
  - auto reload with `dotnet watch`;
  - Swagger UI with JWT sign-in;
  - a local email inbox;
  - unique ports per project, so several projects run side by side.

## What's inside

| Area | Included |
| --- | --- |
| **API** | .NET 10 Minimal APIs, feature folders with a service layer, built-in validation, Swagger UI |
| **Errors** | RFC 7807 ProblemDetails with stable error codes and correlation ids, global exception handling, an error catalog |
| **Configuration** | `.env` files with typed, validated settings classes; `APP_ENV` for dev, stage and prod |
| **Data** | EF Core + PostgreSQL, an initial migration; migrations are always applied manually |
| **Security** | JWT authentication, role policies, password hashing, CORS |
| **Email** | Scriban templates, CSS inlining (PreMailer.Net), MailKit SMTP, Mailpit for local testing |
| **Background jobs** | Hangfire + PostgreSQL: queued, delayed and recurring jobs with retries, and a dashboard; can be switched off in `.env` |
| **Operations** | Health checks, structured logging, correlation ids, one production container (API + worker under supervisor) |
| **Tests** | Unit tests and integration tests (`WebApplicationFactory`), 130+ included |
| **Tooling** | Docker Compose, central package management with lock files, GitHub Actions |

## Quick start

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Docker](https://www.docker.com/products/docker-desktop/).

```bash
# 1. Create the project (outside this repository)
dotnet new dotnet-api-template -n Acme.Billing -o acme-billing
cd acme-billing

# 2. Settings, database and email inbox
cp .env.example .env
docker compose up -d db mailpit

# 3. Tables
dotnet build
dotnet tool restore
dotnet ef database update --project src/Acme.Billing.Api

# 4. Run with auto reload
dotnet watch --project src/Acme.Billing.Api
```

Open `http://localhost:5080/swagger`.

**Naming:**
- `-n` is the .NET name, used for the solution, projects and namespaces. Use PascalCase: `Acme.Billing`.
- `-o` is the folder and repository name. Use lowercase with dashes: `acme-billing`.

| Option | Default | Effect |
| --- | --- | --- |
| `--auto-discovery true` | `false` | Finds feature endpoints and services automatically instead of registering each feature by hand |

## What you get

```text
acme-billing/
├── src/Acme.Billing.Api/          the API: Features/, Common/, Infrastructure/, Data/
├── tests/                         unit and integration tests
├── docs/                          developer guides, ADRs, API conventions
├── docker/                        production and development Dockerfiles
├── docker-compose.yml             PostgreSQL, Mailpit, dev and prod profiles
└── .env.example                   every setting, copy to .env
```

Sample features (Auth, Users, Products) show the patterns end to end. Keep them as references or delete them.

## Documentation

Every generated project includes a guide per topic, covering:
- configuration;
- features;
- database and migrations;
- request data and response shaping;
- errors;
- authentication;
- email;
- background jobs;
- CORS and JSON;
- Swagger;
- logging;
- testing;
- Docker and deployment.

[Browse the docs](https://github.com/code4mk/dotnet-api-template/blob/main/templates/DotnetApiTemplate/docs/README.md).

## Updating

```bash
dotnet new update                                   # newer template version
dotnet new uninstall Code4mk.MinimalApi.Template    # remove
```

Updating the template affects new projects only; existing projects never change.
See the [changelog](https://github.com/code4mk/dotnet-api-template/blob/main/CHANGELOG.md).

## License

[MIT](https://github.com/code4mk/dotnet-api-template/blob/main/LICENSE)
