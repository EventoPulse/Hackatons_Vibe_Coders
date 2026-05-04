using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_RequestedByUserId",
                table: "Conversations",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_RequestedByUserId",
                table: "Conversations",
                column: "RequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_RequestedByUserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_RequestedByUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Conversations");
        }
    }
}
