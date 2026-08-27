namespace CompSci.Core.DTOs;

public class DissertationRequest
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
}

public class DissertationResponse
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
}

/// <summary>
/// Identifies the caller for dissertation access control: Admins can see/manage every record,
/// Lecturers are scoped to only the records they personally created.
/// </summary>
public record DissertationAccessContext(Guid UserId, bool IsAdmin);

/// <summary>
/// Filter criteria for the Admin-only cross-cutting dissertation search/export (spans every
/// lecturer's records). All fields are optional and combine with AND. FromYear/ToYear match
/// against the leading year in AcademicYear (e.g. "2025" in "2025/2026"). Program/Department/School
/// are case-insensitive "contains" matches.
/// </summary>
public class DissertationFilter
{
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public string? Program { get; set; }
    public string? Department { get; set; }
    public string? School { get; set; }
}

/// <summary>
/// Row shape used for the compiled Admin export (CSV/PDF) — intentionally a narrower set of
/// fields than DissertationResponse, per the requested compilation format.
/// </summary>
public class DissertationExportRow
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}
