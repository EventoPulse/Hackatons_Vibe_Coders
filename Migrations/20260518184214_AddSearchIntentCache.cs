using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIntentCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchIntentCacheEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedQuery = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IntentJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HitCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIntentCacheEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIntentCacheEntries_QueryHash",
                table: "SearchIntentCacheEntries",
                column: "QueryHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchIntentCacheEntries");
        }
    }
}
