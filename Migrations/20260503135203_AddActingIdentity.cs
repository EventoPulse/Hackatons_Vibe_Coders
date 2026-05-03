using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddActingIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthorOrganizerProfileId",
                table: "PostComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorType",
                table: "PostComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "PostComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorOrganizerProfileId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorType",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorOrganizerProfileId",
                table: "EventComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorType",
                table: "EventComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessWorkspaceId",
                table: "EventComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_AuthorOrganizerProfileId",
                table: "PostComments",
                column: "AuthorOrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_BusinessWorkspaceId",
                table: "PostComments",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_AuthorOrganizerProfileId",
                table: "Messages",
                column: "AuthorOrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_BusinessWorkspaceId",
                table: "Messages",
                column: "BusinessWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_AuthorOrganizerProfileId",
                table: "EventComments",
                column: "AuthorOrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_BusinessWorkspaceId",
                table: "EventComments",
                column: "BusinessWorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventComments_BusinessWorkspaces_BusinessWorkspaceId",
                table: "EventComments",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventComments_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "EventComments",
                column: "AuthorOrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Messages",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "Messages",
                column: "AuthorOrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_BusinessWorkspaces_BusinessWorkspaceId",
                table: "PostComments",
                column: "BusinessWorkspaceId",
                principalTable: "BusinessWorkspaces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "PostComments",
                column: "AuthorOrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventComments_BusinessWorkspaces_BusinessWorkspaceId",
                table: "EventComments");

            migrationBuilder.DropForeignKey(
                name: "FK_EventComments_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "EventComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_BusinessWorkspaces_BusinessWorkspaceId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_BusinessWorkspaces_BusinessWorkspaceId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_OrganizerProfiles_AuthorOrganizerProfileId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_AuthorOrganizerProfileId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_BusinessWorkspaceId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_Messages_AuthorOrganizerProfileId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_BusinessWorkspaceId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_EventComments_AuthorOrganizerProfileId",
                table: "EventComments");

            migrationBuilder.DropIndex(
                name: "IX_EventComments_BusinessWorkspaceId",
                table: "EventComments");

            migrationBuilder.DropColumn(
                name: "AuthorOrganizerProfileId",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "AuthorType",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "AuthorOrganizerProfileId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AuthorType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AuthorOrganizerProfileId",
                table: "EventComments");

            migrationBuilder.DropColumn(
                name: "AuthorType",
                table: "EventComments");

            migrationBuilder.DropColumn(
                name: "BusinessWorkspaceId",
                table: "EventComments");

        }
    }
}
