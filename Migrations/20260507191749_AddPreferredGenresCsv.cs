using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredGenresCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredGenresCsv",
                table: "UserPreferences",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""UserPreferences""
                SET ""PreferredGenresCsv"" = CASE ""PreferredGenre""
                    WHEN 0 THEN 'Other'
                    WHEN 1 THEN 'Rock'
                    WHEN 2 THEN 'Pop'
                    WHEN 3 THEN 'HipHop'
                    WHEN 4 THEN 'Electronic'
                    WHEN 5 THEN 'Jazz'
                    WHEN 6 THEN 'Classical'
                    WHEN 7 THEN 'Folk'
                    WHEN 8 THEN 'Metal'
                    WHEN 9 THEN 'Theater'
                    WHEN 10 THEN 'Standup'
                    WHEN 11 THEN 'Festival'
                    WHEN 12 THEN 'Exhibition'
                    WHEN 13 THEN 'Sports'
                    WHEN 14 THEN 'Conference'
                    WHEN 15 THEN 'Workshop'
                END
                WHERE ""PreferredGenre"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredGenresCsv",
                table: "UserPreferences");
        }
    }
}
