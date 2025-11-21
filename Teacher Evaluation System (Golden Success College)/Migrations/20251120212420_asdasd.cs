using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class asdasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId1",
                table: "Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_Enrollment_TeacherId1",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "Enrollment");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId",
                table: "Enrollment",
                column: "TeacherId",
                principalTable: "Teacher",
                principalColumn: "TeacherId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId",
                table: "Enrollment");

            migrationBuilder.AddColumn<int>(
                name: "TeacherId1",
                table: "Enrollment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_TeacherId1",
                table: "Enrollment",
                column: "TeacherId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId",
                table: "Enrollment",
                column: "TeacherId",
                principalTable: "Teacher",
                principalColumn: "TeacherId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_Teacher_TeacherId1",
                table: "Enrollment",
                column: "TeacherId1",
                principalTable: "Teacher",
                principalColumn: "TeacherId");
        }
    }
}
