using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                table: "PostComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                table: "EventComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_ParentCommentId",
                table: "PostComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_ParentCommentId",
                table: "EventComments",
                column: "ParentCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventComments_EventComments_ParentCommentId",
                table: "EventComments",
                column: "ParentCommentId",
                principalTable: "EventComments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments",
                column: "ParentCommentId",
                principalTable: "PostComments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventComments_EventComments_ParentCommentId",
                table: "EventComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_ParentCommentId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_EventComments_ParentCommentId",
                table: "EventComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "EventComments");
        }
    }
}
