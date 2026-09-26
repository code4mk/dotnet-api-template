# Documentation

Start with **Getting started**, then read the guide for whatever you're working on.

## Developer guides (`development/`)

| Guide | Read it when you... |
| --- | --- |
| [Getting started](development/getting-started.md) | set up the project for the first time, or need the daily commands |
| [Configuration and environments](development/configuration-and-environments.md) | add or change a setting, switch dev/stage/prod, handle secrets |
| [Adding a feature](development/adding-a-feature.md) | build a new endpoint group end to end (entity → migration → service → endpoints → tests) |
| [Database and migrations](development/database-and-migrations.md) | change the schema, move or backfill data, deploy migrations |
| [Request data](development/request-data.md) | receive route values, query strings, headers, JSON bodies, PATCH and file uploads |
| [Response shaping](development/response-shaping.md) | choose response fields, computed/combined/conditional fields, list vs detail views (like Laravel API Resources) |
| [Errors and exceptions](development/errors-and-exceptions.md) | return or throw errors, add error codes, map library exceptions |
| [Authentication and authorization](development/authentication-and-authorization.md) | protect endpoints, add roles/policies, read the current user |
| [Email](development/email.md) | send an email, write a template, configure SMTP |
| [Background jobs](development/background-jobs.md) | run work in the background, schedule recurring jobs, use the dashboard, run workers |
| [CORS](development/cors.md) | connect a browser frontend on another origin |
| [JSON serialization](development/json-serialization.md) | wonder why a request is rejected, or serialize JSON by hand |
| [API documentation (Swagger)](development/api-documentation.md) | test the API in the browser or improve the generated docs |
| [Logging and correlation ids](development/logging-and-correlation-ids.md) | log from code, trace a request, investigate a reported error |
| [Testing](development/testing.md) | write unit or integration tests |
| [Packages and dependencies](development/packages-and-dependencies.md) | add or update a NuGet package, lock files, security checks |
| [Docker and deployment](development/docker-and-deployment.md) | run everything in Docker, build the production image, deploy |
| [Coding guidelines](development/coding-guidelines.md) | write or review code (rules and PR checklist) |

## Reference

| Document | Contents |
| --- | --- |
| [API conventions](api/conventions.md) | Routes, status codes, error format, pagination: the contract clients rely on |
| [Architecture overview](architecture/overview.md) | Request flow, folders, cross-cutting decisions |
| [ADRs](adr/) | Why the big decisions were made ([template](adr/template.md) for new ones) |
