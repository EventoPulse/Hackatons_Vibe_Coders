using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerValidators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizerValidatorAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizerId = table.Column<string>(type: "text", nullable: false),
                    ValidatorUserId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerValidatorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizerValidatorAssignments_AspNetUsers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizerValidatorAssignments_AspNetUsers_ValidatorUserId",
                        column: x => x.ValidatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventValidatorPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizerValidatorAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerValidatorAssignments_OrganizerId_IsActive",
                table: "OrganizerValidatorAssignments",
                columns: new[] { "OrganizerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerValidatorAssignments_OrganizerId_ValidatorUserId",
                table: "OrganizerValidatorAssignments",
                columns: new[] { "OrganizerId", "ValidatorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerValidatorAssignments_ValidatorUserId_IsActive",
                table: "OrganizerValidatorAssignments",
                columns: new[] { "ValidatorUserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventValidatorPermissions");

            migrationBuilder.DropTable(
                name: "OrganizerValidatorAssignments");
        }
    }
}
