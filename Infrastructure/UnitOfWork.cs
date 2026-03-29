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

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Courses = new CourseRepository(_context);
        Assignments = new AssignmentRepository(_context);
        PastQuestions = new PastQuestionRepository(_context);
        Notes = new NoteRepository(_context);
        Students = new StudentRepository(_context);
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
