using EventsApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260509113000_AddLayoutSectionColor")]
    public partial class AddLayoutSectionColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "LayoutSections"
                ADD COLUMN IF NOT EXISTS "ColorHex" character varying(16) NOT NULL DEFAULT '#2456ff';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "LayoutSections"
                DROP COLUMN IF EXISTS "ColorHex";
                """);
        }
    }
}
