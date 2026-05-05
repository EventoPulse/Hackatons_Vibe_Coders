using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class ScopeValidatorsToOrganizerPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventValidatorPermissions");

            migrationBuilder.AddColumn<int>(
                name: "OrganizerProfileId",
                table: "OrganizerValidatorAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "OrganizerValidatorAssignments"
                SET "IsActive" = FALSE,
                    "UpdatedAt" = CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
                WHERE "OrganizerProfileId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerValidatorAssignments_OrganizerProfileId_IsActive",
                table: "OrganizerValidatorAssignments",
                columns: new[] { "OrganizerProfileId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizerValidatorAssignments_OrganizerProfiles_OrganizerPr~",
                table: "OrganizerValidatorAssignments",
                column: "OrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizerValidatorAssignments_OrganizerProfiles_OrganizerPr~",
                table: "OrganizerValidatorAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrganizerValidatorAssignments_OrganizerProfileId_IsActive",
                table: "OrganizerValidatorAssignments");

            migrationBuilder.DropColumn(
                name: "OrganizerProfileId",
                table: "OrganizerValidatorAssignments");

            migrationBuilder.CreateTable(
                name: "EventValidatorPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    OrganizerValidatorAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventValidatorPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventValidatorPermissions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventValidatorPermissions_OrganizerValidatorAssignments_Org~",
                        column: x => x.OrganizerValidatorAssignmentId,
                        principalTable: "OrganizerValidatorAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventValidatorPermissions_EventId",
                table: "EventValidatorPermissions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventValidatorPermissions_OrganizerValidatorAssignmentId_Ev~",
                table: "EventValidatorPermissions",
                columns: new[] { "OrganizerValidatorAssignmentId", "EventId" },
                unique: true);
        }
    }
}
