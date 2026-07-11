using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PagueVeloz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceIdToAccountOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountOperations_AccountId",
                table: "AccountOperations");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceId",
                table: "AccountOperations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AccountOperations_AccountId_ReferenceId",
                table: "AccountOperations",
                columns: new[] { "AccountId", "ReferenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountOperations_AccountId_ReferenceId",
                table: "AccountOperations");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "AccountOperations");

            migrationBuilder.CreateIndex(
                name: "IX_AccountOperations_AccountId",
                table: "AccountOperations",
                column: "AccountId");
        }
    }
}
