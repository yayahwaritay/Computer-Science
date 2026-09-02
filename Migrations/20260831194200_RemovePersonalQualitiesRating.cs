using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersonalQualitiesRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalQualities",
                table: "InternshipEvaluations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalQualities",
                table: "InternshipEvaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
