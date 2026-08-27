using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KampusKayipEsya.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManageTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "manage_token_hash",
                table: "items",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "manage_token_hash",
                table: "items");
        }
    }
}
