using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfOrganizer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletionReflectionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionChallenges",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionLessonsLearned",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionReflection",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionChallenges",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionLessonsLearned",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionReflection",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionChallenges",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompletionLessonsLearned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompletionReflection",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompletionChallenges",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "CompletionLessonsLearned",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "CompletionReflection",
                table: "Goals");
        }
    }
}
