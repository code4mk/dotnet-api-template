# Packages and dependencies

NuGet packages are managed in two committed places:

| File | What it holds |
| --- | --- |
| `Directory.Packages.props` | **Every package version**, once for the whole solution (central package management) |
| `**/packages.lock.json` | **Every package actually used**, also transitive ones, with its exact version and content hash |

`.csproj` files only name the packages they use, without versions:
`<PackageReference Include="MailKit" />`.

## Lock files

`Directory.Build.props` turns on `RestorePackagesWithLockFile`, so each project (API and both test projects)
has a `packages.lock.json`. It records the full dependency graph, including packages you never referenced
yourself, and a content hash for each one.

| Without lock files | With lock files |
| --- | --- |
| Transitive versions are resolved again on every restore | Exactly the recorded versions, on every machine, in CI and in Docker |
| A package with the same version but different content is accepted | The content hash must match, or restore fails |
| Dependency changes are invisible in pull requests | Every change shows up in `packages.lock.json` |

**Locked restores** (`dotnet restore --locked-mode`) use exactly what's in the lock files and fail with
`NU1004` if a project or version changed without updating them. They run in:

- CI (`.github/workflows/build-and-test.yml`), which also checks that the lock files are committed and
  unchanged (locked mode would silently create a missing one);
- the production Docker build (`docker/Dockerfile`).

Local `dotnet build` / `dotnet watch` restore normally and keep the lock files up to date.

## Add a package

```bash
# 1. The version, in Directory.Packages.props (in the matching ItemGroup):
#      <PackageVersion Include="Polly" Version="8.8.0" />
# 2. The reference, in the project that uses it:
#      <PackageReference Include="Polly" />
dotnet restore                                   # updates packages.lock.json
dotnet list package --vulnerable --include-transitive
git add Directory.Packages.props src/**/*.csproj **/packages.lock.json
```

`dotnet add package Polly` also works: with central package management it adds the version to
`Directory.Packages.props` and the reference to the project.

Before adding a package, check it's maintained (recent releases, many downloads), has a compatible license,
and has no open advisories.

## Update a package

```bash
# 1. Change the version in Directory.Packages.props
# 2. Re-resolve and rewrite the lock files:
dotnet restore --force-evaluate
dotnet build && dotnet test
dotnet list package --vulnerable --include-transitive
# 3. Commit Directory.Packages.props and every changed packages.lock.json together
```

Plain `dotnet restore` after a version change fails in locked mode (CI) with `NU1004` until the lock files are
updated with `--force-evaluate`.

List what can be updated:

```bash
dotnet list package --outdated
```

Update packages that belong together at the same time (all `Microsoft.AspNetCore.*` and
`Microsoft.EntityFrameworkCore.*` share one version; `Hangfire.*` too).

## Transitive pins

Some packages bring an old or vulnerable version of another package. Central package management then pins
it in `Directory.Packages.props` (`CentralPackageTransitivePinningEnabled` is on):

| Pin | Why |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Relational` | Npgsql depends on an older EF Core Relational; keep it on the EF Core version |
| `Newtonsoft.Json` 13.0.4 | Hangfire.Core depends on 11.0.1, which has a known vulnerability (GHSA-5crp-9r3c-p9vr) |

When a direct package is updated past the problem, the pin can be removed (re-check with
`dotnet list package --vulnerable --include-transitive`).

## Security checks

```bash
dotnet list package --vulnerable --include-transitive    # known advisories, including transitive packages
dotnet list package --deprecated
```

The build also reports vulnerable packages as warnings (`NU1901`–`NU1904`) during restore. Treat them as
errors: update or pin the package.

### Automated updates (optional)

GitHub Dependabot understands central package management and lock files. Add `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: /
    schedule:
      interval: weekly
    groups:
      aspnetcore-efcore:
        patterns: ["Microsoft.AspNetCore.*", "Microsoft.EntityFrameworkCore.*"]
      hangfire:
        patterns: ["Hangfire.*"]
```

Each update then arrives as a pull request with `Directory.Packages.props` and the lock files changed, and CI
runs the locked restore and tests on it.

## Troubleshooting

| Error | Fix |
| --- | --- |
| `NU1004: The package reference X version has changed ...` / `... lock file is out of date` | A version or project changed without updating the lock file: `dotnet restore --force-evaluate`, commit the lock files |
| CI: `packages.lock.json missing or changed` | The lock files weren't committed after a change: run `dotnet restore --force-evaluate` locally and commit them |
| `NU1403: Package content hash validation failed` | The package on the feed doesn't match the recorded hash. Don't bypass it: clear the cache (`dotnet nuget locals all --clear`) and retry; if it persists, the package or feed was changed and needs investigating |
| `NU1010: The PackageReference items X do not have corresponding PackageVersion` | Add `<PackageVersion Include="X" Version="..." />` to `Directory.Packages.props` |
| `NU1008: Projects that use central package version management should not define the version` | Remove `Version="..."` from the `PackageReference` in the `.csproj` |
