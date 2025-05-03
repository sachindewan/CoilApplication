using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coil.Api.Migrations
{
    /// <inheritdoc />
    public partial class updatedenquiryentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<BigInteger>(
                name: "MobileNumber",
                table: "Enquiries",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MobileNumber",
                table: "Enquiries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(BigInteger),
                oldType: "numeric");
        }
    }
}
