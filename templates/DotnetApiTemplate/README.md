# DotnetApiTemplate

ASP.NET Core Minimal API built on .NET 10.

## Quick start

```bash
cp .env.example .env
docker compose up -d db mailpit
dotnet tool restore
dotnet ef database update --project src/DotnetApiTemplate.Api
dotnet watch --project src/DotnetApiTemplate.Api
```

`dotnet watch` reloads the API when you save a file.

Then open Swagger UI at `http://localhost:5080/swagger`, or send the sample requests in
`src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http` from your IDE.

Full instructions: [docs/development/getting-started.md](docs/development/getting-started.md). All developer guides
(configuration, database and migrations, errors, auth, email, CORS, testing, deployment, ...): [docs/README.md](docs/README.md).

## Solution layout

| Path | Contents |
| --- | --- |
| `src/DotnetApiTemplate.Api` | The API: features, domain, data, infrastructure, common |
| `tests/DotnetApiTemplate.UnitTests` | Service tests (EF Core in-memory) |
| `tests/DotnetApiTemplate.IntegrationTests` | Endpoint tests with `WebApplicationFactory` |
| `docker/` | `Dockerfile` (production) and `Dockerfile.dev` (auto reload); `docker-compose.yml` is at the root |
| `docs/` | Developer guides, API conventions, architecture, ADRs; start at `docs/README.md` |
| `scripts/` | Helper scripts (migrations, local setup) |
| `.github/workflows/` | CI build/test and Docker image publishing |

## Key decisions

- [ADR-0001: Minimal APIs](docs/adr/0001-use-minimal-apis.md)
- [ADR-0002: Folder structure](docs/adr/0002-project-folder-structure.md)
- [API conventions](docs/api/conventions.md)
- [Coding guidelines](docs/development/coding-guidelines.md)

## Sample features

| Feature | Endpoints | Access |
| --- | --- | --- |
| Auth | `POST /api/auth/login` | Public |
| Users | `POST /api/users` (register) | Public |
| Users | `GET /api/users`, `GET/PUT /api/users/{id}` | Authenticated |
| Users | `DELETE /api/users/{id}` | Admin |
| Products | `GET /api/products`, `GET /api/products/{id}` | Public |
| Products | `POST`, `PUT`, `DELETE` | Authenticated |

Delete the sample features you don't need, or copy `Products` to start a new feature.
