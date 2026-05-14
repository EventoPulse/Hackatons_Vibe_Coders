using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsApp.Migrations
{
    /// <inheritdoc />
    public partial class AcceptPendingConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Conversations"
                SET "Status" = 1,
                    "RespondedAt" = COALESCE("RespondedAt", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
                    "UpdatedAt" = CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
                WHERE "Status" <> 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No reliable down migration: previously gated conversations should stay usable.
        }
    }
}
