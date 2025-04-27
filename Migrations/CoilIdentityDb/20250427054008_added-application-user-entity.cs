using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coil.Api.Migrations.CoilIdentityDb
{
    /// <inheritdoc />
    public partial class addedapplicationuserentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "AspNetUsers");
        }
    }
}
