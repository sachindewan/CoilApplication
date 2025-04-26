using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coil.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChallengestateRenamedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChallengeState",
                table: "ChallengesStates");

            migrationBuilder.AddColumn<bool>(
                name: "State",
                table: "ChallengesStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "ChallengesStates");

            migrationBuilder.AddColumn<string>(
                name: "ChallengeState",
                table: "ChallengesStates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
