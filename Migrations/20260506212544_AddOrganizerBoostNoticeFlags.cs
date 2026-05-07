using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerBoostNoticeFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FirstApprovalBoostGranted",
                table: "OrganizerData",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FirstApprovalBoostNoticeSeen",
                table: "OrganizerData",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstApprovalBoostGranted",
                table: "OrganizerData");

            migrationBuilder.DropColumn(
                name: "FirstApprovalBoostNoticeSeen",
                table: "OrganizerData");
        }
    }
}
