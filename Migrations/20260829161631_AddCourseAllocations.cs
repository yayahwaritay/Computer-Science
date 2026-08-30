using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    ProgramName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    YearOfStudy = table.Column<int>(type: "integer", nullable: false),
                    CourseCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CourseDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreditHours = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StaffName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LecturerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAllocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAllocations_AcademicYear_Semester",
                table: "CourseAllocations",
                columns: new[] { "AcademicYear", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAllocations_LecturerUserId",
                table: "CourseAllocations",
                column: "LecturerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseAllocations");
        }
    }
}
