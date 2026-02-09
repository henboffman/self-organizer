using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfOrganizer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerPlansAndUrlFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CareerPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Milestones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedGoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedSkillIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedProjectIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedHabitIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_UserId",
                table: "CareerPlans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Goals");
        }
    }
}
