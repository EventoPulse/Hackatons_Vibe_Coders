using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    public partial class AddSeatAndTicketPriceColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Seats"
                ADD COLUMN IF NOT EXISTS "ColorHex" character varying(16);
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "TicketSectionPrices"
                ADD COLUMN IF NOT EXISTS "ColorHex" character varying(16);
            """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_TicketSectionPrices_TicketId_SectionId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TicketSectionPrices_TicketId_SectionId_ColorHex"
                ON "TicketSectionPrices" ("TicketId", "SectionId", "ColorHex");
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_TicketSectionPrices_TicketId_SectionId_ColorHex";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TicketSectionPrices_TicketId_SectionId"
                ON "TicketSectionPrices" ("TicketId", "SectionId");
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "TicketSectionPrices"
                DROP COLUMN IF EXISTS "ColorHex";
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "Seats"
                DROP COLUMN IF EXISTS "ColorHex";
            """);
        }
    }
}
