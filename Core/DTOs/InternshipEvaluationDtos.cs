using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

/// <summary>Submitted by an Organization account to evaluate one student's internship.</summary>
public class InternshipEvaluationRequest
{
    /// <summary>The student's human-readable Student ID (the Students.StudentId column, e.g. "24807") - not the internal database ID.</summary>
    public string StudentId { get; set; } = string.Empty;
    public string CompanySupervisorName { get; set; } = string.Empty;
    public string CompanySupervisorPhone { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; } = Semester.Second;
    public DateTime InternshipStartDate { get; set; }
    public int InternshipMonths { get; set; }

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

    public string? OtherRatingLabel { get; set; }
    public int? OtherRatingScore { get; set; }

    public string? Comments { get; set; }
    public string SupervisorSignatureName { get; set; } = string.Empty;
    public DateTime CertificationDate { get; set; }
}

/// <summary>Submitted by the allocated Lecturer (or Admin) to grade the internship report out of 30.</summary>
public class ReportGradeRequest
{
    public decimal ReportScore { get; set; }
}

public class InternshipEvaluationResponse
{
    public Guid Id { get; set; }

    /// <summary>The student's internal database ID (Students.Id) - see StudentIdNumber for the human-readable Student ID.</summary>
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }

    public Guid OrganizationUserId { get; set; }
    public string? OrganizationName { get; set; }
    public string CompanySupervisorName { get; set; } = string.Empty;
    public string CompanySupervisorPhone { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; }
    public DateTime InternshipStartDate { get; set; }
    public int InternshipMonths { get; set; }

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

    public string? OtherRatingLabel { get; set; }
    public int? OtherRatingScore { get; set; }

    public string? Comments { get; set; }
    public string SupervisorSignatureName { get; set; } = string.Empty;
    public DateTime CertificationDate { get; set; }

    public int RawRatingTotal { get; set; }
    public decimal EvaluationScore { get; set; }

    public Guid? AllocatedLecturerUserId { get; set; }
    public string? AllocatedLecturerUsername { get; set; }
    public decimal? ReportScore { get; set; }
    public Guid? ReportGradedByUserId { get; set; }
    public DateTime? ReportGradedAt { get; set; }

    public decimal? TotalScore { get; set; }
    public string? Grade { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Identifies the caller for internship evaluation access control: Admin sees/manages every
/// record; Organization is scoped to records it submitted; Lecturer is scoped to records
/// allocated to it for report grading.
/// </summary>
public record InternshipEvaluationAccessContext(Guid UserId, string Role)
{
    public bool IsAdmin => Role == "Admin";
    public bool IsLecturer => Role == "Lecturer";
    public bool IsOrganization => Role == "Organization";
}

/// <summary>One row of the compiled per-program grade report: (Name, ID, Evaluation, Report, Grade).</summary>
public class CompiledGradeRow
{
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public decimal EvaluationScore { get; set; }
    public decimal? ReportScore { get; set; }
    public string? Grade { get; set; }
}

public class CompiledGradeReport
{
    public string ProgramName { get; set; } = string.Empty;
    public List<CompiledGradeRow> Rows { get; set; } = new();
}

/// <summary>Optional filter for the compiled per-program report. All fields combine with AND.</summary>
public class InternshipEvaluationFilter
{
    public string? ProgramName { get; set; }
    public string? AcademicYear { get; set; }
    public Semester? Semester { get; set; }
}
