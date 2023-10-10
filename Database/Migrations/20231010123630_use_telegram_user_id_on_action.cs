using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class use_telegram_user_id_on_action : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_UserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_UserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Actions");

            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "Actions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_TelegramUserId",
                table: "Actions",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Actions_TelegramUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "Actions");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Actions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_UserId",
                table: "Actions",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_UserId",
                table: "Actions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
