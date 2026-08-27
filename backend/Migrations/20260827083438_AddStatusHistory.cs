using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KampusKayipEsya.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_status_history_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_location",
                table: "items",
                column: "location");

            migrationBuilder.CreateIndex(
                name: "IX_item_status_history_item_id",
                table: "item_status_history",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_status_history_timestamp",
                table: "item_status_history",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_status_history");

            migrationBuilder.DropIndex(
                name: "IX_items_location",
                table: "items");
        }
    }
}
