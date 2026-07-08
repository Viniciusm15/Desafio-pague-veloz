using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PagueVeloz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyAndMetadataToAccountOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "AccountOperations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AccountOperations",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AccountOperations");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AccountOperations");
        }
    }
}
