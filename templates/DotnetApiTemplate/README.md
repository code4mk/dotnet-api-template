# DotnetApiTemplate

ASP.NET Core Minimal API built on .NET 10.

## Quick start

```bash
cd docker && docker compose --env-file ../.env up -d db && cd ..
dotnet watch --project src/DotnetApiTemplate.Api
```

`dotnet watch` reloads the API when you save a file.

Then open `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http` in your IDE and send the sample requests, or browse
`http://localhost:5080/openapi/v1.json`.

Full instructions: [docs/development/getting-started.md](docs/development/getting-started.md).

## Solution layout

| Path | Contents |
| --- | --- |
| `src/DotnetApiTemplate.Api` | The API: features, domain, data, infrastructure, common |
| `tests/DotnetApiTemplate.UnitTests` | Service tests (EF Core in-memory) |
| `tests/DotnetApiTemplate.IntegrationTests` | Endpoint tests with `WebApplicationFactory` |
| `docker/` | Dockerfile and Compose files |
| `docs/` | ADRs, architecture, API conventions, developer guides |
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
