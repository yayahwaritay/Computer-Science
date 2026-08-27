namespace CompSci.Core.Entities;

/// <summary>
/// Site-wide audit trail entry: records who (Admin/Lecturer) performed a create/update/delete
/// action on which record, and when. Written automatically by the API layer's activity-logging filter.
/// </summary>
public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;

    /// <summary>Snapshot of the actor's Lecturer ID at the time of the action, if they were a Lecturer.</summary>
    public string? LecturerId { get; set; }

    /// <summary>e.g. "Dissertation", "Course", "Assignment", "Note", "PastQuestion", "Student".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>"Create", "Update", or "Delete".</summary>
    public string Action { get; set; } = string.Empty;

    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
