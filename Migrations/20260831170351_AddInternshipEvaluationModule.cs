using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipEvaluationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CredentialsExpireAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InternshipAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LecturerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternshipAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternshipAllocations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InternshipEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentFullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StudentIdNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProgramName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    OrganizationUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanySupervisorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanySupervisorPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    InternshipStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InternshipMonths = table.Column<int>(type: "integer", nullable: false),
                    RapportWithSupervisor = table.Column<int>(type: "integer", nullable: false),
                    RapportWithStaffAndClient = table.Column<int>(type: "integer", nullable: false),
                    CommunicatesWell = table.Column<int>(type: "integer", nullable: false),
                    SeeksNewKnowledge = table.Column<int>(type: "integer", nullable: false),
                    ShowsInitiative = table.Column<int>(type: "integer", nullable: false),
                    ManagesTimeWell = table.Column<int>(type: "integer", nullable: false),
                    ProducesAccurateReports = table.Column<int>(type: "integer", nullable: false),
                    DemonstratesAdequateKnowledge = table.Column<int>(type: "integer", nullable: false),
                    DressesProfessionally = table.Column<int>(type: "integer", nullable: false),
                    PersonalQualities = table.Column<int>(type: "integer", nullable: false),
                    IsPunctual = table.Column<int>(type: "integer", nullable: false),
                    IsDependable = table.Column<int>(type: "integer", nullable: false),
                    AcceptsConstructiveCriticism = table.Column<int>(type: "integer", nullable: false),
                    DemonstratesEnthusiasm = table.Column<int>(type: "integer", nullable: false),
                    OtherRatingLabel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OtherRatingScore = table.Column<int>(type: "integer", nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupervisorSignatureName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CertificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawRatingTotal = table.Column<int>(type: "integer", nullable: false),
                    EvaluationScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AllocatedLecturerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ReportGradedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportGradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Grade = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternshipEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternshipEvaluations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternshipAllocations_LecturerUserId",
                table: "InternshipAllocations",
                column: "LecturerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternshipAllocations_StudentId_AcademicYear_Semester",
                table: "InternshipAllocations",
                columns: new[] { "StudentId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternshipEvaluations_AllocatedLecturerUserId",
                table: "InternshipEvaluations",
                column: "AllocatedLecturerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternshipEvaluations_OrganizationUserId",
                table: "InternshipEvaluations",
                column: "OrganizationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternshipEvaluations_StudentId",
                table: "InternshipEvaluations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_UserId",
                table: "Organizations",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternshipAllocations");

            migrationBuilder.DropTable(
                name: "InternshipEvaluations");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropColumn(
                name: "CredentialsExpireAt",
                table: "Users");
        }
    }
}
