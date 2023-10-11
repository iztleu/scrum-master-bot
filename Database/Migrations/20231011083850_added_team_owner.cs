using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class added_team_owner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "ScrumTeams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScrumTeams_OwnerId",
                table: "ScrumTeams",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrumTeams_Users_OwnerId",
                table: "ScrumTeams",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrumTeams_Users_OwnerId",
                table: "ScrumTeams");

            migrationBuilder.DropIndex(
                name: "IX_ScrumTeams_OwnerId",
                table: "ScrumTeams");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ScrumTeams");
        }
    }
}
