using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseSetup.Migrations
{
    /// <inheritdoc />
    public partial class TenantRowSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(TenantRowSecuritySql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Removing RLS requires a separately reviewed recovery plan.");
    }
}
