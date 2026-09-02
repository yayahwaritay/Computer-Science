namespace CompSci.Core.Enums;

public enum UserRole
{
    Admin = 0,
    Lecturer = 1,
    Student = 2,

    /// <summary>
    /// A host organization/company account registered by Admin/Lecturer so it can submit
    /// internship evaluations for the students it hosted. See <see cref="Entities.Organization"/>.
    /// </summary>
    Organization = 3
}
