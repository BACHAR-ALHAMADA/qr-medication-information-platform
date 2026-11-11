using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseSetup.Migrations
{
    /// <inheritdoc />
    public partial class InitialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organizations");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "capabilities",
                schema: "organizations",
                columns: table => new
                {
                    capability_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capabilities", x => x.capability_code);
                    table.CheckConstraint("ck_capability_code", "capability_code IN ('workspace.read','delegation.scope.evaluate')");
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "organizations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.organization_id);
                    table.CheckConstraint("ck_org_status", "status IN ('active','suspended','closed')");
                    table.CheckConstraint("ck_organizations_organization_id_nonempty", "organization_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "staff_users",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_users", x => x.user_id);
                    table.CheckConstraint("ck_staff_user_nonempty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "organizations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => new { x.organization_id, x.branch_id });
                    table.CheckConstraint("ck_branches_branch_id_nonempty", "branch_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_branches_organization_id_nonempty", "organization_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "FK_branches_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                schema: "organizations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorization_revision = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => new { x.organization_id, x.user_id });
                    table.CheckConstraint("ck_membership_revision", "authorization_revision > 0");
                    table.CheckConstraint("ck_membership_status", "status IN ('active','suspended','removed')");
                    table.CheckConstraint("ck_memberships_organization_id_nonempty", "organization_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_memberships_user_id_nonempty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "FK_memberships_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membership_branch_grants",
                schema: "organizations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_branch_grants", x => new { x.organization_id, x.user_id, x.branch_id });
                    table.CheckConstraint("ck_membership_branch_grants_branch_id_nonempty", "branch_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_membership_branch_grants_organization_id_nonempty", "organization_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_membership_branch_grants_user_id_nonempty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "FK_membership_branch_grants_branches_organization_id_branch_id",
                        columns: x => new { x.organization_id, x.branch_id },
                        principalSchema: "organizations",
                        principalTable: "branches",
                        principalColumns: new[] { "organization_id", "branch_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membership_branch_grants_memberships_organization_id_user_id",
                        columns: x => new { x.organization_id, x.user_id },
                        principalSchema: "organizations",
                        principalTable: "memberships",
                        principalColumns: new[] { "organization_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membership_capabilities",
                schema: "organizations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "text", nullable: false),
                    is_delegable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_capabilities", x => new { x.organization_id, x.user_id, x.capability_code });
                    table.CheckConstraint("ck_delegable", "NOT is_delegable OR capability_code = 'workspace.read'");
                    table.CheckConstraint("ck_membership_capabilities_organization_id_nonempty", "organization_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_membership_capabilities_user_id_nonempty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "FK_membership_capabilities_capabilities_capability_code",
                        column: x => x.capability_code,
                        principalSchema: "organizations",
                        principalTable: "capabilities",
                        principalColumn: "capability_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membership_capabilities_memberships_organization_id_user_id",
                        columns: x => new { x.organization_id, x.user_id },
                        principalSchema: "organizations",
                        principalTable: "memberships",
                        principalColumns: new[] { "organization_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "organizations",
                table: "capabilities",
                column: "capability_code",
                values: new object[]
                {
                    "delegation.scope.evaluate",
                    "workspace.read"
                });

            migrationBuilder.CreateIndex(
                name: "IX_membership_branch_grants_organization_id_branch_id_user_id",
                schema: "organizations",
                table: "membership_branch_grants",
                columns: new[] { "organization_id", "branch_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membership_capabilities_capability_code",
                schema: "organizations",
                table: "membership_capabilities",
                column: "capability_code");

            migrationBuilder.CreateIndex(
                name: "IX_memberships_user_id_organization_id",
                schema: "organizations",
                table: "memberships",
                columns: new[] { "user_id", "organization_id" });
            migrationBuilder.Sql(InitialFoundationSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Destructive foundation rollback requires a separately reviewed recovery plan.");
    }
}