namespace CompSci.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICourseRepository Courses { get; }
    IAssignmentRepository Assignments { get; }
    IPastQuestionRepository PastQuestions { get; }
    INoteRepository Notes { get; }
    IStudentRepository Students { get; }
    IDissertationRepository Dissertations { get; }
    IActivityLogRepository ActivityLogs { get; }
    ICourseAllocationRepository CourseAllocations { get; }
    IOrganizationRepository Organizations { get; }
    IInternshipAllocationRepository InternshipAllocations { get; }
    IInternshipEvaluationRepository InternshipEvaluations { get; }
    IDissertationAllocationRepository DissertationAllocations { get; }
    IStudentSubmissionRepository StudentSubmissions { get; }
    ISubmissionCommentRepository SubmissionComments { get; }
    Task<int> SaveChangesAsync();
}
