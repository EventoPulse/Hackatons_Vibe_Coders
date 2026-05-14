using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationArchiveColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedByP1At",
                table: "Conversations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedByP2At",
                table: "Conversations",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedByP1At",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ArchivedByP2At",
                table: "Conversations");
        }
    }
}
