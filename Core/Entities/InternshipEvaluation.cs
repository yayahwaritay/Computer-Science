using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

/// <summary>
/// Digital equivalent of the paper "Student Internship Evaluation Form": the host organization
/// rates the student on 13 fixed criteria (1-4 each, "Poor" to "Excellent"), which are summed to
/// <see cref="RawRatingTotal"/> (0-52) and scaled to <see cref="EvaluationScore"/> (0-70). The
/// internship-supervising lecturer separately grades the written report out of 30
/// (<see cref="ReportScore"/>); once both exist, <see cref="TotalScore"/> and <see cref="Grade"/>
/// are computed via GradeCalculator.
/// </summary>
public class InternshipEvaluation
{
    public Guid Id { get; set; }

    // Student snapshot at submission time (mirrors Dissertation's denormalized fields), so the
    // record and any compiled report stay stable even if the Student profile changes later.
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }

    public Guid OrganizationUserId { get; set; }
    public string CompanySupervisorName { get; set; } = string.Empty;
    public string CompanySupervisorPhone { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty; // e.g. "2025/2026" — drives compiled-report filtering
    public Semester Semester { get; set; } = Semester.Second;
    public DateTime InternshipStartDate { get; set; }
    public int InternshipMonths { get; set; }

    // The 13 fixed rating criteria from the paper form, each 1 (Poor) - 4 (Excellent).
    public int RapportWithSupervisor { get; set; }
    public int RapportWithStaffAndClient { get; set; }
    public int CommunicatesWell { get; set; }
    public int SeeksNewKnowledge { get; set; }
    public int ShowsInitiative { get; set; }
    public int ManagesTimeWell { get; set; }
    public int ProducesAccurateReports { get; set; }
    public int DemonstratesAdequateKnowledge { get; set; }
    public int DressesProfessionally { get; set; }
    public int IsPunctual { get; set; }
    public int IsDependable { get; set; }
    public int AcceptsConstructiveCriticism { get; set; }
    public int DemonstratesEnthusiasm { get; set; }

    /// <summary>Open "Other ratings, please specify" row — recorded but excluded from the ÷52 total.</summary>
    public string? OtherRatingLabel { get; set; }
    public int? OtherRatingScore { get; set; }

    public string? Comments { get; set; }
    public string SupervisorSignatureName { get; set; } = string.Empty;
    public DateTime CertificationDate { get; set; }

    /// <summary>Sum of the 13 fixed ratings, 0-52.</summary>
    public int RawRatingTotal { get; set; }

    /// <summary>RawRatingTotal / 52 * 70, rounded to 2 decimal places, 0-70.</summary>
    public decimal EvaluationScore { get; set; }

    // Lecturer-graded report component (30 marks) — filled in later.
    public Guid? AllocatedLecturerUserId { get; set; }
    public decimal? ReportScore { get; set; }
    public Guid? ReportGradedByUserId { get; set; }
    public DateTime? ReportGradedAt { get; set; }

    /// <summary>EvaluationScore + ReportScore, 0-100. Null until the report is graded.</summary>
    public decimal? TotalScore { get; set; }

    /// <summary>A-F per the fixed grade table. Null until the report is graded.</summary>
    public string? Grade { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
