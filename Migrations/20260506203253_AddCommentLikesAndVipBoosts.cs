using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentLikesAndVipBoosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserActivities_EventId",
                table: "UserActivities");

            migrationBuilder.DropIndex(
                name: "IX_UserActivities_PostId",
                table: "UserActivities");

            migrationBuilder.AddColumn<int>(
                name: "VipBoostCreditsAvailable",
                table: "OrganizerData",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "VipBoostCreditsUsed",
                table: "OrganizerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EventBoosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    OrganizerId = table.Column<string>(type: "text", nullable: false),
                    CreditsSpent = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventBoosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventBoosts_AspNetUsers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventBoosts_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventCommentLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventCommentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCommentLikes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventCommentLikes_EventComments_EventCommentId",
                        column: x => x.EventCommentId,
                        principalTable: "EventComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostCommentLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostCommentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostCommentLikes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostCommentLikes_PostComments_PostCommentId",
                        column: x => x.PostCommentId,
                        principalTable: "PostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_EventId_ActivityType_CreatedAt",
                table: "UserActivities",
                columns: new[] { "EventId", "ActivityType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_PostId_ActivityType_CreatedAt",
                table: "UserActivities",
                columns: new[] { "PostId", "ActivityType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventBoosts_EventId_CreatedAt",
                table: "EventBoosts",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventBoosts_OrganizerId_CreatedAt",
                table: "EventBoosts",
                columns: new[] { "OrganizerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventCommentLikes_EventCommentId_UserId",
                table: "EventCommentLikes",
                columns: new[] { "EventCommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventCommentLikes_UserId",
                table: "EventCommentLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentLikes_PostCommentId_UserId",
                table: "PostCommentLikes",
                columns: new[] { "PostCommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentLikes_UserId",
                table: "PostCommentLikes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventBoosts");

            migrationBuilder.DropTable(
                name: "EventCommentLikes");

            migrationBuilder.DropTable(
                name: "PostCommentLikes");

            migrationBuilder.DropIndex(
                name: "IX_UserActivities_EventId_ActivityType_CreatedAt",
                table: "UserActivities");

            migrationBuilder.DropIndex(
                name: "IX_UserActivities_PostId_ActivityType_CreatedAt",
                table: "UserActivities");

            migrationBuilder.DropColumn(
                name: "VipBoostCreditsAvailable",
                table: "OrganizerData");

            migrationBuilder.DropColumn(
                name: "VipBoostCreditsUsed",
                table: "OrganizerData");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_EventId",
                table: "UserActivities",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_PostId",
                table: "UserActivities",
                column: "PostId");
        }
    }
}
