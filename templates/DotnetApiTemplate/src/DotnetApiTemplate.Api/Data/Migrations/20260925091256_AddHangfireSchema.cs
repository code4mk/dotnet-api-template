using Microsoft.EntityFrameworkCore.Migrations;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

#nullable disable

namespace DotnetApiTemplate.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHangfireSchema : Migration
    {
        /// <summary>
        /// Hangfire's tables in schema "hangfire" (versions 3..23), the same SQL as Hangfire.PostgreSql's installer.
        /// Pinned to explicit versions: a later Hangfire.PostgreSql schema version gets its own migration.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(HangfireSchema.InstallSql(fromVersion: 3, toVersion: 23));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(HangfireSchema.DropSql);
    }
}
