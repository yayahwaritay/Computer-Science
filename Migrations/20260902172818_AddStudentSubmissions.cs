using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DissertationAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LecturerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DissertationAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DissertationAllocations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSubmissions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionComments_StudentSubmissions_StudentSubmissionId",
                        column: x => x.StudentSubmissionId,
                        principalTable: "StudentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DissertationAllocations_LecturerUserId",
                table: "DissertationAllocations",
                column: "LecturerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DissertationAllocations_StudentId_AcademicYear",
                table: "DissertationAllocations",
                columns: new[] { "StudentId", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubmissions_StudentId_Type",
                table: "StudentSubmissions",
                columns: new[] { "StudentId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionComments_StudentSubmissionId",
                table: "SubmissionComments",
                column: "StudentSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DissertationAllocations");

            migrationBuilder.DropTable(
                name: "SubmissionComments");

            migrationBuilder.DropTable(
                name: "StudentSubmissions");
        }
    }
}
