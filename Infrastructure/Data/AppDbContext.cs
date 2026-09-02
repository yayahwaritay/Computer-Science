using CompSci.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<PastQuestion> PastQuestions => Set<PastQuestion>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Dissertation> Dissertations => Set<Dissertation>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<CourseAllocation> CourseAllocations => Set<CourseAllocation>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<InternshipAllocation> InternshipAllocations => Set<InternshipAllocation>();
    public DbSet<InternshipEvaluation> InternshipEvaluations => Set<InternshipEvaluation>();
    public DbSet<DissertationAllocation> DissertationAllocations => Set<DissertationAllocation>();
    public DbSet<StudentSubmission> StudentSubmissions => Set<StudentSubmission>();
    public DbSet<SubmissionComment> SubmissionComments => Set<SubmissionComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.LecturerId).IsUnique().HasFilter("\"LecturerId\" IS NOT NULL");
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LecturerId).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsApproved).IsRequired();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CourseCode).IsUnique();
            entity.Property(e => e.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CourseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Staff).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CourseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.AssignmentTitle).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<PastQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CourseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CourseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentId).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StudentId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProgramName).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Student>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Dissertation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.Property(e => e.CreatedByUserId).IsRequired();
            entity.Property(e => e.StudentName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.StudentId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Program).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Department).HasMaxLength(200).IsRequired();
            entity.Property(e => e.School).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Topic).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Grade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserRole).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LecturerId).HasMaxLength(20);
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<CourseAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AcademicYear, e.Semester });
            entity.HasIndex(e => e.LecturerUserId);
            entity.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProgramName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CourseDescription).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CreditHours).HasMaxLength(10).IsRequired();
            entity.Property(e => e.StaffName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Organization>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InternshipAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.AcademicYear, e.Semester }).IsUnique();
            entity.HasIndex(e => e.LecturerUserId);
            entity.HasIndex(e => e.OrganizationUserId);
            entity.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InternshipEvaluation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.OrganizationUserId);
            entity.HasIndex(e => e.AllocatedLecturerUserId);
            entity.Property(e => e.StudentFullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.StudentIdNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProgramName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CompanySupervisorName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CompanySupervisorPhone).HasMaxLength(30).IsRequired();
            entity.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OtherRatingLabel).HasMaxLength(300);
            entity.Property(e => e.Comments).HasMaxLength(2000);
            entity.Property(e => e.SupervisorSignatureName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Grade).HasMaxLength(1);
            entity.Property(e => e.EvaluationScore).HasPrecision(5, 2);
            entity.Property(e => e.ReportScore).HasPrecision(5, 2);
            entity.Property(e => e.TotalScore).HasPrecision(5, 2);

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DissertationAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.AcademicYear }).IsUnique();
            entity.HasIndex(e => e.LecturerUserId);
            entity.Property(e => e.AcademicYear).HasMaxLength(20).IsRequired();

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.Type }).IsUnique();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubmissionComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentSubmissionId);
            entity.Property(e => e.Text).HasMaxLength(2000).IsRequired();

            entity.HasOne(e => e.StudentSubmission)
                .WithMany()
                .HasForeignKey(e => e.StudentSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
