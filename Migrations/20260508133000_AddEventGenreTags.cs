using EventsApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260508133000_AddEventGenreTags")]
    public partial class AddEventGenreTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Events"
                ADD COLUMN IF NOT EXISTS "GenreTags" character varying(512);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "EventSeries"
                ADD COLUMN IF NOT EXISTS "GenreTags" character varying(512);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Events"
                DROP COLUMN IF EXISTS "GenreTags";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "EventSeries"
                DROP COLUMN IF EXISTS "GenreTags";
                """);
        }
    }
}
