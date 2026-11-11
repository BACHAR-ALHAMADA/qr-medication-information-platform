using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseSetup.Migrations
{
    /// <inheritdoc />
    public partial class IdentityMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authentication_issuers",
                schema: "identity",
                columns: table => new
                {
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "statement_timestamp()"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    issuer = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    revision = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "statement_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authentication_issuers", x => x.issuer_id);
                    table.CheckConstraint("ck_issuer_id", "issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_issuer_revision", "revision > 0");
                    table.CheckConstraint("ck_issuer_value", "octet_length(issuer) BETWEEN 1 AND 1024 AND issuer ~* '^https://[^/?#@[:space:]]+(/[^?#[:space:]]*)?$' AND issuer !~ U&'[\\0001-\\0020\\007F-\\00A0\\1680\\2000-\\200A\\2028\\2029\\202F\\205F\\3000]'");
                });

            migrationBuilder.CreateTable(
                name: "external_staff_identities",
                schema: "identity",
                columns: table => new
                {
                    mapping_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "statement_timestamp()"),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    state = table.Column<string>(type: "text", nullable: false, defaultValue: "disabled"),
                    subject = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "statement_timestamp()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_staff_identities", x => x.mapping_id);
                    table.CheckConstraint("ck_mapping_ids", "mapping_id <> '00000000-0000-0000-0000-000000000000'::uuid AND issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid AND user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_mapping_revision", "revision > 0");
                    table.CheckConstraint("ck_mapping_state", "state IN ('active','disabled','unlinked')");
                    table.CheckConstraint("ck_mapping_subject", "octet_length(subject) BETWEEN 1 AND 1024 AND subject !~ U&'[\\0001-\\001F\\007F-\\009F]' AND btrim(subject, U&'\\0020\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') <> ''");
                    table.ForeignKey(
                        name: "FK_external_staff_identities_authentication_issuers_issuer_id",
                        column: x => x.issuer_id,
                        principalSchema: "identity",
                        principalTable: "authentication_issuers",
                        principalColumn: "issuer_id",
                        onDelete: ReferentialAction.Restrict,
                        onUpdate: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_external_staff_identities_staff_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "staff_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict,
                        onUpdate: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authentication_issuers_issuer",
                schema: "identity",
                table: "authentication_issuers",
                column: "issuer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_staff_identities_issuer_id_subject",
                schema: "identity",
                table: "external_staff_identities",
                columns: new[] { "issuer_id", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_staff_identities_user_id",
                schema: "identity",
                table: "external_staff_identities",
                column: "user_id");
            migrationBuilder.Sql(IdentityMappingsSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Identity mapping rollback requires a separately approved recovery plan.");
    }
}
