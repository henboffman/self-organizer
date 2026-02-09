using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfOrganizer.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillIds",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "LinkedSkillIds",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "NextActionTaskId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedSkillIds",
                table: "Habits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "LinkedSkillIds",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "NextActionTaskId",
                table: "Goals",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillIds",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LinkedSkillIds",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "NextActionTaskId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LinkedSkillIds",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "LinkedSkillIds",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "NextActionTaskId",
                table: "Goals");
        }
    }
}
