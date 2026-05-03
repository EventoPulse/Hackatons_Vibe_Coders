using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "Stories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizerProfileId",
                table: "Stories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizerProfileId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "OrganizerProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "OrganizerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForWorkspace",
                table: "OrganizerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLegalBusinessNamePublicly",
                table: "OrganizerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOwnerProfilePublicly",
                table: "OrganizerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrganizerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessWorkspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillingEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    StripeConnectedAccountId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    StripeOnboardingStatus = table.Column<int>(type: "int", nullable: false),
                    PayoutsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ChargesEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PaymentProvider = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessWorkspaces_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BusinessWorkspaceId",
                table: "Transactions",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_BusinessWorkspaceId",
                table: "Stories",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_OrganizerProfileId",
                table: "Stories",
                column: "OrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BusinessWorkspaceId",
                table: "Posts",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_OrganizerProfileId",
                table: "Posts",
                column: "OrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerProfiles_BusinessWorkspaceId_IsDefaultForWorkspace",
                table: "OrganizerProfiles",
                columns: new[] { "BusinessWorkspaceId", "IsDefaultForWorkspace" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_BusinessWorkspaceId",
                table: "Events",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWorkspaces_OwnerId_IsDefault",
                table: "BusinessWorkspaces",
                columns: new[] { "OwnerId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWorkspaces_StripeConnectedAccountId",
                table: "BusinessWorkspaces",
                column: "StripeConnectedAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Events",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizerProfiles_BusinessWorkspaces_BusinessWorkspaceId",
                table: "OrganizerProfiles",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Posts",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_OrganizerProfiles_OrganizerProfileId",
                table: "Posts",
                column: "OrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Stories",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_OrganizerProfiles_OrganizerProfileId",
                table: "Stories",
                column: "OrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Transactions",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizerProfiles_BusinessWorkspaces_BusinessWorkspaceId",
                table: "OrganizerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_OrganizerProfiles_OrganizerProfileId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_OrganizerProfiles_OrganizerProfileId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "BusinessWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BusinessWorkspaceId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Stories_BusinessWorkspaceId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_OrganizerProfileId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Posts_BusinessWorkspaceId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_OrganizerProfileId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_OrganizerProfiles_BusinessWorkspaceId_IsDefaultForWorkspace",
                table: "OrganizerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Events_BusinessWorkspaceId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "OrganizerProfileId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OrganizerProfileId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "IsDefaultForWorkspace",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "ShowLegalBusinessNamePublicly",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "ShowOwnerProfilePublicly",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrganizerProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "Events");
        }
    }
}
