# Migrations

EF Core migrations live in this folder. `InitialCreate` creates the users and products tables.

The API never applies migrations at startup. Apply them yourself from the repository root:

```bash
dotnet tool restore
dotnet ef database update --project src/DotnetApiTemplate.Api
```

After changing an entity or configuration, add a migration, review the generated code, then apply it:

```bash
dotnet ef migrations add <Name> --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
dotnet ef database update --project src/DotnetApiTemplate.Api
```

Or use the helper scripts: `./scripts/add-migration.ps1 -Name <Name>` and `./scripts/update-database.ps1`.
For production, generate a reviewed SQL script with `dotnet ef migrations script --idempotent`.
