using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class TES1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Student",
                newName: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_LevelId",
                table: "Student",
                column: "LevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Level_LevelId",
                table: "Student",
                column: "LevelId",
                principalTable: "Level",
                principalColumn: "LevelId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_Level_LevelId",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_LevelId",
                table: "Student");

            migrationBuilder.RenameColumn(
                name: "LevelId",
                table: "Student",
                newName: "Level");
        }
    }
}
