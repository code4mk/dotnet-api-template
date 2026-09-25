# Database and migrations

PostgreSQL through EF Core. The schema is changed **only by migrations that you apply yourself**:
the API never creates tables, migrates or seeds data at startup.

## Where things live

| Path | Contents |
| --- | --- |
| `Domain/Entities/` | Entity classes (`User`, `Product`), deriving from `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`) |
| `Data/Configurations/` | One `IEntityTypeConfiguration<T>` per entity: table, lengths, indexes (picked up automatically) |
| `Data/AppDbContext.cs` | `DbSet`s; sets `CreatedAt`/`UpdatedAt` (UTC) in `SaveChangesAsync` |
| `Data/Migrations/` | Generated migrations; `InitialCreate` creates the users and products tables, `AddHangfireSchema` the background job tables (schema `hangfire`) |

The connection string is built from `DB_*` in `.env` (`DatabaseSettings`). `dotnet ef` uses the same
settings, so commands run against the database in your `.env`.

## Setup

```bash
dotnet tool restore                                             # installs dotnet-ef (once per machine)
docker compose up -d db                                         # local PostgreSQL
dotnet ef database update --project src/DotnetApiTemplate.Api   # apply all migrations
```

All `dotnet ef` commands below need `--project src/DotnetApiTemplate.Api`; run them from the
repository root. On Windows, `./scripts/add-migration.ps1 -Name <Name>` and
`./scripts/update-database.ps1` wrap the two common ones.

## Command reference

| Task | Command |
| --- | --- |
| Apply all pending migrations | `dotnet ef database update` |
| Create a migration | `dotnet ef migrations add <Name> --output-dir Data/Migrations` |
| List migrations (and which are applied) | `dotnet ef migrations list` |
| Check the model has no un-migrated changes | `dotnet ef migrations has-pending-model-changes` |
| Remove the last migration (not applied yet) | `dotnet ef migrations remove` |
| Roll the database back to a migration | `dotnet ef database update <PreviousMigrationName>` |
| Roll back everything | `dotnet ef database update 0` |
| SQL script for production (safe to re-run) | `dotnet ef migrations script --idempotent --output migrations.sql` |
| Self-contained migration runner | `dotnet ef migrations bundle --self-contained --output efbundle` |
| Reset the local database | `docker compose down -v && docker compose up -d db`, then `database update` |

## Changing the schema

1. Change the entity (`Domain/Entities/`) and its configuration (`Data/Configurations/`).
2. Create a migration with a descriptive name (`AddProductSku`, `MakeUserPhoneRequired`, not `Update1`):

   ```bash
   dotnet ef migrations add AddProductSku --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
   ```

3. **Review the generated `Up` and `Down`** before applying. Look for:
   - `DropColumn`/`DropTable` you didn't intend: a rename often shows up as drop + add, which loses data.
     Replace it with `RenameColumn`/`RenameTable`.
   - `AlterColumn` that shortens a column or makes it `NOT NULL` while existing rows would violate it.
   - Indexes on large tables (see [Production](#production)).
4. Apply it locally: `dotnet ef database update --project src/DotnetApiTemplate.Api`.
5. Commit the three files together: `<timestamp>_<Name>.cs`, `<timestamp>_<Name>.Designer.cs` and
   the updated `AppDbContextModelSnapshot.cs`.

Rules:

- Never edit a migration that is already applied anywhere shared (staging, production, a teammate's
  database). Add a new migration instead.
- Never delete migrations or edit the snapshot by hand.
- If your branch and `main` both added migrations, `AppDbContextModelSnapshot.cs` conflicts. Take `main`'s
  version, delete your migration files, and run `migrations add` again so yours is generated on top.

## Data migrations

Schema migrations change the shape; data migrations change rows. Pick the approach by the kind of data.

### 1. Backfill when changing the schema (most common)

Adding a required column to a table with rows can't be done in one step. Use **expand → backfill → contract**,
inside one migration (small tables) or across several releases (large tables, zero downtime):

```csharp
public partial class AddProductSku : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Expand: add the column as nullable
        migrationBuilder.AddColumn<string>(name: "Sku", table: "Products", maxLength: 50, nullable: true);

        // 2. Backfill existing rows with plain SQL
        migrationBuilder.Sql("""UPDATE "Products" SET "Sku" = 'SKU-' || "Id" WHERE "Sku" IS NULL;""");

        // 3. Contract: now it can be required and unique
        migrationBuilder.AlterColumn<string>(name: "Sku", table: "Products", maxLength: 50, nullable: false,
            oldClrType: typeof(string), oldMaxLength: 50, oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Products_Sku", table: "Products", column: "Sku", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Products_Sku", table: "Products");
        migrationBuilder.DropColumn(name: "Sku", table: "Products");
    }
}
```

Generate the migration with `migrations add` first (it writes the `AddColumn`), then edit it into this shape.

### 2. Reference data (lookup values every environment needs)

Countries, statuses, fixed categories: put them in the entity configuration with `HasData`. EF writes
the inserts/updates/deletes into the next migration and keeps them in sync:

```csharp
// Data/Configurations/CategoryConfiguration.cs
builder.HasData(
    new Category { Id = 1, Name = "Hardware" },
    new Category { Id = 2, Name = "Software" });
```

Use fixed ids, and only for small, static data. `HasData` bypasses `SaveChangesAsync`, so set
`CreatedAt` to a fixed UTC date yourself (otherwise it is `0001-01-01`).

### 3. One-off data fixes

Correcting bad rows, moving data between columns: write a migration with only `migrationBuilder.Sql(...)`:

```bash
dotnet ef migrations add FixDuplicateProductNames --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
```

```csharp
protected override void Up(MigrationBuilder migrationBuilder) =>
    migrationBuilder.Sql("""
        UPDATE "Products" SET "Name" = "Name" || ' (' || "Id" || ')'
        WHERE "Name" IN (SELECT "Name" FROM "Products" GROUP BY "Name" HAVING COUNT(*) > 1);
        """);

protected override void Down(MigrationBuilder migrationBuilder) { }   // not reversible: say so
```

### 4. Large data changes

Millions of rows don't belong in a migration (long locks, timeouts, one giant transaction). Write a
separate script or background job that works in batches, and run it after the schema migration.

### 5. Development and demo data

Nothing is seeded automatically. For local test data, keep an SQL script (for example `scripts/dev-data.sql`,
never run against production) and load it on demand:

```bash
docker compose exec -T db psql -U postgres -d appdb < scripts/dev-data.sql
```

Or create data through the API (`.http` file, Swagger), which also exercises validation and business rules.

### Rules for data migrations

- **Never use `AppDbContext` or entity classes inside a migration.** They reflect *today's* model;
  migrations must keep working years later. Use `migrationBuilder.Sql(...)`, `InsertData`, `UpdateData`, `DeleteData`.
- Table and column names are PascalCase and must be quoted in PostgreSQL SQL: `"Products"."Name"`.
- Make SQL safe to re-run where you can (`WHERE "Sku" IS NULL`, `ON CONFLICT DO NOTHING`).
- Write a `Down` that undoes the change, or leave it empty with a comment when the change can't be undone.

## Hangfire's tables

Background jobs are stored in their own schema, `hangfire`, created by the migration `AddHangfireSchema`
(the same SQL as Hangfire.PostgreSql's installer; Hangfire never changes the schema at startup). You don't
touch these tables; when a Hangfire.PostgreSql update adds a schema version, a test tells you to add a
migration. See [Background jobs](background-jobs.md#upgrading-hangfirepostgresql).

## Production

- **Never run `database update` from a developer machine against production.** Generate an artifact,
  review it, and let the deployment run it:

  ```bash
  # Option A: reviewed SQL script (idempotent: skips migrations already applied)
  dotnet ef migrations script --idempotent --project src/DotnetApiTemplate.Api --output migrations.sql

  # Option B: a bundle executable, run in the pipeline with the production connection
  dotnet ef migrations bundle --self-contained --project src/DotnetApiTemplate.Api --output efbundle
  ./efbundle --connection "Host=...;Database=...;Username=...;Password=..."
  ```

- Back up the database before applying.
- Run migrations **before** deploying code that needs them. For zero downtime, keep each migration
  compatible with the currently running code (expand first, remove old columns in a later release).
- Creating an index on a big table locks writes; use a raw SQL migration with
  `CREATE INDEX CONCURRENTLY` (and `suppressTransaction: true` on `migrationBuilder.Sql`).

## Troubleshooting

| Output | Meaning |
| --- | --- |
| `No migrations were applied. The database is already up to date.` | Nothing to do |
| Why doesn't the first update on an empty database log EF's "history table missing" error? | `Data/MissingHistoryTableInterceptor.cs` answers EF's first look at `__EFMigrationsHistory` when the table doesn't exist yet (only while `dotnet ef` runs), so a `fail:` line always means a real problem |
| An error **after** `Applying migration 'X'` and no `Done.` | Migration X failed and was rolled back; the `fail: ... Failed executing DbCommand` line above it shows its SQL |
| `relation "..." already exists` | The table was created outside migrations (or by an old `EnsureCreated`): drop it locally (`docker compose down -v`) and update again |

## Useful SQL

```bash
docker compose exec db psql -U postgres -d appdb                    # open psql
```

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;         -- applied migrations
UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = 'you@example.com'; -- make a user admin
```
