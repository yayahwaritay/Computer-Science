namespace CompSci.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICourseRepository Courses { get; }
    IAssignmentRepository Assignments { get; }
    IPastQuestionRepository PastQuestions { get; }
    INoteRepository Notes { get; }
    IStudentRepository Students { get; }
    Task<int> SaveChangesAsync();
}
