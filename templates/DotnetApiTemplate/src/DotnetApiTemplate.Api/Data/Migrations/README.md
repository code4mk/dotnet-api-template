# Migrations

EF Core migrations are generated into this folder.

Create the first migration from the repository root:

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
```

Or use the helper script: `./scripts/add-migration.ps1 -Name InitialCreate`.

Until a migration exists, the Development startup creates the schema with
`EnsureCreated` so you can run the app immediately. Once you add migrations,
startup applies them with `Migrate` instead.
