using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hangfire.PostgreSql;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>
/// Hangfire's database tables, created by EF Core migrations (<c>dotnet ef database update</c>) instead of by
/// Hangfire at startup. Builds the same SQL as Hangfire.PostgreSql's own installer, from the scripts embedded in
/// the package, for an explicit version range.
/// </summary>
public static partial class HangfireSchema
{
    public const string SchemaName = "hangfire";

    /// <summary>
    /// The newest Hangfire schema version applied by this project's migrations. When a Hangfire.PostgreSql update
    /// ships a newer version, a test fails: add a migration with <c>InstallSql(MigratedVersion + 1, LatestVersion)</c>
    /// and raise this constant.
    /// </summary>
    public const int MigratedVersion = 23;

    /// <summary>The newest schema version shipped in the installed Hangfire.PostgreSql package.</summary>
    public static int LatestVersion => ScriptVersions().Max();

    /// <summary>
    /// SQL that applies Hangfire schema versions <paramref name="fromVersion"/>..<paramref name="toVersion"/>,
    /// exactly like <c>PostgreSqlObjectsInstaller</c>: each script, then the version bump. It resets
    /// <c>search_path</c> at the end, because Hangfire's scripts change it.
    /// </summary>
    public static string InstallSql(int fromVersion, int toVersion)
    {
        var available = ScriptVersions().ToHashSet();
        var sql = new StringBuilder();

        for (var version = fromVersion; version <= toVersion; version++)
        {
            if (!available.Contains(version))
            {
                throw new InvalidOperationException($"Hangfire.PostgreSql has no schema script v{version}.");
            }

            var previous = version == 3 ? 1 : version - 1;   // the installer's first script moves the version from 1 to 3
            sql.AppendLine(ReadScript(version)).AppendLine(";")
               .AppendLine(CultureInfo.InvariantCulture, $"""UPDATE "{SchemaName}"."schema" SET "version" = {version} WHERE "version" = {previous};""");
        }

        sql.AppendLine("RESET search_path;");
        return sql.ToString();
    }

    public const string DropSql = $"""DROP SCHEMA IF EXISTS "{SchemaName}" CASCADE;""";

    private static IEnumerable<int> ScriptVersions() => typeof(PostgreSqlStorage).Assembly
        .GetManifestResourceNames()
        .Select(name => ScriptName().Match(name))
        .Where(match => match.Success)
        .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));

    private static string ReadScript(int version)
    {
        using var stream = typeof(PostgreSqlStorage).Assembly
            .GetManifestResourceStream($"Hangfire.PostgreSql.Scripts.Install.v{version.ToString(CultureInfo.InvariantCulture)}.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"^Hangfire\.PostgreSql\.Scripts\.Install\.v(\d+)\.sql$")]
    private static partial Regex ScriptName();
}
