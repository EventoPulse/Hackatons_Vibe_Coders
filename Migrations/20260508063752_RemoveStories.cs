using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    BusinessWorkspaceId = table.Column<int>(type: "integer", nullable: true),
                    OrganizerProfileId = table.Column<int>(type: "integer", nullable: true),
                    Caption = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stories_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stories_BusinessWorkspaces_BusinessWorkspaceId",
                        column: x => x.BusinessWorkspaceId,
                        principalTable: "BusinessWorkspaces",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Stories_OrganizerProfiles_OrganizerProfileId",
                        column: x => x.OrganizerProfileId,
                        principalTable: "OrganizerProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_AuthorId",
                table: "Stories",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_BusinessWorkspaceId",
                table: "Stories",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_OrganizerProfileId",
                table: "Stories",
                column: "OrganizerProfileId");
        }
    }
}
