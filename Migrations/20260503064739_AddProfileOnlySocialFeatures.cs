using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileOnlySocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PinnedEventId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileStatusEmoji",
                table: "AspNetUsers",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileStatusText",
                table: "AspNetUsers",
                type: "nvarchar(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileStatusUpdatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileStatusVisibility",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserProfileSharedEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileSharedEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfileSharedEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileSharedEvents_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PinnedEventId",
                table: "AspNetUsers",
                column: "PinnedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileSharedEvents_EventId",
                table: "UserProfileSharedEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileSharedEvents_UserId_CreatedAt",
                table: "UserProfileSharedEvents",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileSharedEvents_UserId_EventId",
                table: "UserProfileSharedEvents",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Events_PinnedEventId",
                table: "AspNetUsers",
                column: "PinnedEventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Events_PinnedEventId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserProfileSharedEvents");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PinnedEventId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PinnedEventId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileStatusEmoji",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileStatusText",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileStatusUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileStatusVisibility",
                table: "AspNetUsers");
        }
    }
}
