# Maintaining and publishing

Notes for template maintainers: how to change, test and publish the template. This file isn't part of the NuGet
package.

## Repository layout

```text
dotnet-api-template/
├── README.md                     public readme (also shown on nuget.org)
├── CHANGELOG.md                  template version history
├── nuget-publish.md              this guide
├── TemplatePack.csproj           packs the template into a NuGet package
├── .github/workflows/
│   ├── template-ci.yml           creates sample projects, builds and tests them (manual run)
│   └── publish.yml               publishes the package to nuget.org (manual run)
└── templates/
    └── DotnetApiTemplate/        the template itself (everything a new project gets)
        ├── .template.config/template.json
        ├── DotnetApiTemplate.sln
        └── src/ tests/ docker/ docs/ scripts/ .github/ ...
```

The `.github/workflows` inside `templates/DotnetApiTemplate` belong to generated projects. They don't run in this
repository.

## Rules

- Keep `DotnetApiTemplate` as the placeholder name everywhere. Never rename it.
- Keep `dotnetapitemplate-slug` where a lowercase, dash-only name is needed (Docker, Compose).
- Set package versions only in `templates/DotnetApiTemplate/Directory.Packages.props`, and commit the updated
  `packages.lock.json` files.
- Never create projects inside this repository.

## Install from source

```bash
dotnet new install ./templates/DotnetApiTemplate            # first time
dotnet new install ./templates/DotnetApiTemplate --force    # after changes
dotnet new uninstall ./templates/DotnetApiTemplate          # remove
dotnet new list dotnet-api-template                         # check
```

## Workflow for a change

1. Create a branch and edit files under `templates/DotnetApiTemplate`.
2. Test locally, outside the repository:

   ```bash
   dotnet new install ./templates/DotnetApiTemplate --force
   cd ..
   dotnet new dotnet-api-template -n Test.Project -o test-project
   cd test-project && dotnet build && dotnet test
   cd .. && rm -rf test-project
   ```

   Also test `--auto-discovery true` when changing feature wiring.
3. Add an entry to `CHANGELOG.md`.
4. Open a pull request. Optionally run `template-ci` from the Actions tab.
5. After merging, publish the new version.

## Publishing to nuget.org

Packages are published under the nuget.org account **Code4mk** as `Code4mk.MinimalApi.Template`.

### From GitHub Actions

1. Add a nuget.org API key as the repository secret `NUGET_API_KEY` (Settings → Secrets and variables → Actions).
2. Open Actions → `publish` → **Run workflow** and enter the version (e.g. `1.1.0`).

The workflow packs the template with that version and pushes it. It doesn't run automatically; uncomment the
`push` trigger in `publish.yml` to publish on version tags.

### From your machine

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

### Notes

- Create the API key on nuget.org (username → API Keys) with the **Push** scope and glob pattern `Code4mk.*`.
- New versions appear 15 to 30 minutes after validation.
- Every publish needs a new version number. A broken version can be unlisted but never deleted.
- `README.md` is packed into the package and shown on nuget.org: keep its links absolute (GitHub URLs).
- Check the package contents before pushing: open the `.nupkg` (a zip) and confirm no `.env`, `bin/` or `obj/`.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `No templates found matching: 'dotnet-api-template'` | Install the template and check `dotnet new list dotnet-api-template` |
| Namespaces like `my_app.Api` | Dashes were used in `-n`. Use PascalCase in `-n` and dashes only in `-o` |
| New project contains another project | `dotnet new` ran inside the template repo. Delete it and run from another folder |
| `409 Conflict` on push | That version already exists on nuget.org: bump the version |
