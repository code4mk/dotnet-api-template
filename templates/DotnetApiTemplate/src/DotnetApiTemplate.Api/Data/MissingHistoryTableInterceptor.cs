using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DotnetApiTemplate.Api.Data;

/// <summary>
/// On an empty database, <c>dotnet ef database update</c> first reads "__EFMigrationsHistory" before creating it,
/// so the query fails and EF logs "fail: ... Failed executing DbCommand" although everything then works.
/// This interceptor answers that one query with "no migrations applied yet" when the table doesn't exist,
/// which is what EF concludes from the failure anyway. Every other command, including a migration that
/// really fails, runs and is logged normally. Registered only while <c>dotnet ef</c> runs (EF.IsDesignTime).
/// </summary>
internal sealed class MissingHistoryTableInterceptor : DbCommandInterceptor
{
    private const string HistoryTable = "__EFMigrationsHistory";

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) =>
        IsHistoryQuery(command) && !HistoryTableExists(command)
            ? InterceptionResult<DbDataReader>.SuppressWithResult(EmptyHistory())
            : result;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default) =>
        IsHistoryQuery(command) && !await HistoryTableExistsAsync(command, cancellationToken)
            ? InterceptionResult<DbDataReader>.SuppressWithResult(EmptyHistory())
            : result;

    private static bool IsHistoryQuery(DbCommand command) =>
        command.CommandText.TrimStart().StartsWith("SELECT \"MigrationId\", \"ProductVersion\"", StringComparison.Ordinal)
        && command.CommandText.Contains($"FROM \"{HistoryTable}\"", StringComparison.Ordinal);

    private static bool HistoryTableExists(DbCommand command)
    {
        using var check = ExistsCommand(command);
        return check.ExecuteScalar() is true;
    }

    private static async Task<bool> HistoryTableExistsAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var check = ExistsCommand(command);
        return await check.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static DbCommand ExistsCommand(DbCommand command)
    {
        var check = command.Connection!.CreateCommand();
        check.Transaction = command.Transaction;
        check.CommandText = $"SELECT to_regclass('\"{HistoryTable}\"') IS NOT NULL";
        return check;
    }

    /// <summary>An empty result with the history table's columns: "no migrations applied".</summary>
    private static DbDataReader EmptyHistory()
    {
        var table = new DataTable();
        table.Columns.Add("MigrationId", typeof(string));
        table.Columns.Add("ProductVersion", typeof(string));
        return table.CreateDataReader();
    }
}
