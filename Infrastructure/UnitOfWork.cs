using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using CompSci.Infrastructure.Repositories;

namespace CompSci.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public ICourseRepository Courses { get; }
    public IAssignmentRepository Assignments { get; }
    public IPastQuestionRepository PastQuestions { get; }
    public INoteRepository Notes { get; }
    public IStudentRepository Students { get; }
    public IDissertationRepository Dissertations { get; }
    public IActivityLogRepository ActivityLogs { get; }
    public ICourseAllocationRepository CourseAllocations { get; }
    public IOrganizationRepository Organizations { get; }
    public IInternshipAllocationRepository InternshipAllocations { get; }
    public IInternshipEvaluationRepository InternshipEvaluations { get; }
    public IDissertationAllocationRepository DissertationAllocations { get; }
    public IStudentSubmissionRepository StudentSubmissions { get; }
    public ISubmissionCommentRepository SubmissionComments { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Courses = new CourseRepository(_context);
        Assignments = new AssignmentRepository(_context);
        PastQuestions = new PastQuestionRepository(_context);
        Notes = new NoteRepository(_context);
        Students = new StudentRepository(_context);
        Dissertations = new DissertationRepository(_context);
        ActivityLogs = new ActivityLogRepository(_context);
        CourseAllocations = new CourseAllocationRepository(_context);
        Organizations = new OrganizationRepository(_context);
        InternshipAllocations = new InternshipAllocationRepository(_context);
        InternshipEvaluations = new InternshipEvaluationRepository(_context);
        DissertationAllocations = new DissertationAllocationRepository(_context);
        StudentSubmissions = new StudentSubmissionRepository(_context);
        SubmissionComments = new SubmissionCommentRepository(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
