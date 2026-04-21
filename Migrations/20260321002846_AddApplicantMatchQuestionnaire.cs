using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdoptionAgency.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantMatchQuestionnaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMatchRunAt",
                table: "Applicants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchQuestionnaireJson",
                table: "Applicants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMatchRunAt",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "MatchQuestionnaireJson",
                table: "Applicants");
        }
    }
}
