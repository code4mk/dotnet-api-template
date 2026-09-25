using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Jobs;

public sealed class HangfireSchemaTests
{
    [Fact]
    public void Migrations_CoverEverySchemaVersionOfTheInstalledHangfirePackage()
    {
        Assert.True(HangfireSchema.LatestVersion == HangfireSchema.MigratedVersion,
            $"Hangfire.PostgreSql ships schema v{HangfireSchema.LatestVersion}, the migrations stop at v{HangfireSchema.MigratedVersion}. " +
            $"Add a migration whose Up runs HangfireSchema.InstallSql({HangfireSchema.MigratedVersion + 1}, {HangfireSchema.LatestVersion}), " +
            "then raise HangfireSchema.MigratedVersion. See docs/development/background-jobs.md.");
    }

    [Fact]
    public void InstallSql_CreatesTheSchemaBumpsTheVersionAndResetsSearchPath()
    {
        var sql = HangfireSchema.InstallSql(3, HangfireSchema.MigratedVersion);

        Assert.Contains("CREATE SCHEMA \"hangfire\"", sql);
        Assert.Contains($"SET \"version\" = {HangfireSchema.MigratedVersion} WHERE \"version\" = {HangfireSchema.MigratedVersion - 1};", sql);
        Assert.EndsWith("RESET search_path;", sql.TrimEnd());
    }

    [Fact]
    public void InstallSql_ForUnknownVersion_Throws() =>
        Assert.Throws<InvalidOperationException>(() => HangfireSchema.InstallSql(3, HangfireSchema.LatestVersion + 1));
}
