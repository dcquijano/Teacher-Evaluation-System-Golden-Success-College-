using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teacher_Evaluation_System__Golden_Success_College_.Migrations
{
    /// <inheritdoc />
    public partial class CreateEvaluatuonPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvaluationPeriodId",
                table: "Evaluation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EvaluationPeriod",
                columns: table => new
                {
                    EvaluationPeriodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPeriod", x => x.EvaluationPeriodId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_EvaluationPeriodId",
                table: "Evaluation",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPeriod_AcademicYear_Semester",
                table: "EvaluationPeriod",
                columns: new[] { "AcademicYear", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPeriod_IsCurrent",
                table: "EvaluationPeriod",
                column: "IsCurrent");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluation_EvaluationPeriod_EvaluationPeriodId",
                table: "Evaluation",
                column: "EvaluationPeriodId",
                principalTable: "EvaluationPeriod",
                principalColumn: "EvaluationPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluation_EvaluationPeriod_EvaluationPeriodId",
                table: "Evaluation");

            migrationBuilder.DropTable(
                name: "EvaluationPeriod");

            migrationBuilder.DropIndex(
                name: "IX_Evaluation_EvaluationPeriodId",
                table: "Evaluation");

            migrationBuilder.DropColumn(
                name: "EvaluationPeriodId",
                table: "Evaluation");
        }
    }
}
