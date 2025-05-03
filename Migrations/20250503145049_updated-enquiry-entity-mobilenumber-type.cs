using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coil.Api.Migrations
{
    /// <inheritdoc />
    public partial class updatedenquiryentitymobilenumbertype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "MobileNumber",
                table: "Enquiries",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(BigInteger),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<BigInteger>(
                name: "MobileNumber",
                table: "Enquiries",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
