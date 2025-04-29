using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coil.Api.Migrations
{
    /// <inheritdoc />
    public partial class updaterawmaterialquantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "RawMaterialQuantities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialQuantities_PlantId",
                table: "RawMaterialQuantities",
                column: "PlantId");

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterialQuantities_Plants_PlantId",
                table: "RawMaterialQuantities",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "PlantId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterialQuantities_Plants_PlantId",
                table: "RawMaterialQuantities");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterialQuantities_PlantId",
                table: "RawMaterialQuantities");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "RawMaterialQuantities");
        }
    }
}
