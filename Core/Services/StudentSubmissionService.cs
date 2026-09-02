using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Email;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Services;

public class StudentSubmissionService : IStudentSubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailSender _emailSender;

    public StudentSubmissionService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _emailSender = emailSender;
    }

    public async Task<StudentSubmissionResponse> UploadAsync(SubmissionType type, Guid studentUserId, IFormFile file)
    {
        ValidateFile(file);

        var student = await _unitOfWork.Students.GetByUserIdAsync(studentUserId)
            ?? throw new InvalidOperationException("No student profile found for this account.");

        var folder = type == SubmissionType.InternshipReport ? "internship-reports" : "dissertation-submissions";
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        string filePath;
        using (var stream = file.OpenReadStream())
        {
            filePath = await _fileStorageService.SaveFileAsync(stream, fileName, folder);
        }

        var submission = await _unitOfWork.StudentSubmissions.GetForStudentAndTypeAsync(student.Id, type);
        if (submission != null)
        {
            // Re-upload: overwrite the previous file in place rather than creating a new record.
            await _fileStorageService.DeleteFileAsync(submission.FilePath);
            submission.FilePath = filePath;
            submission.OriginalFileName = file.FileName;
            submission.SubmissionCount += 1;
            submission.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.StudentSubmissions.UpdateAsync(submission);
        }
        else
        {
            submission = new StudentSubmission
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Type = type,
                FilePath = filePath,
                OriginalFileName = file.FileName,
                SubmissionCount = 1,
                SubmittedAt = DateTime.UtcNow
            };
            await _unitOfWork.StudentSubmissions.AddAsync(submission);
        }

        await _unitOfWork.SaveChangesAsync();

        await NotifyAssignedLecturersAsync(type, student, submission);

        return await MapToResponseAsync(submission, student);
    }

    public async Task<StudentSubmissionResponse?> GetMineAsync(SubmissionType type, Guid studentUserId)
    {
        var student = await _unitOfWork.Students.GetByUserIdAsync(studentUserId);
        if (student == null)
            return null;

        var submission = await _unitOfWork.StudentSubmissions.GetForStudentAndTypeAsync(student.Id, type);
        return submission == null ? null : await MapToResponseAsync(submission, student);
    }

    public async Task<IEnumerable<StudentSubmissionResponse>> GetAllAsync(SubmissionType type, SubmissionAccessContext access)
    {
        var submissions = await _unitOfWork.StudentSubmissions.GetAllByTypeAsync(type);

        if (!access.IsAdmin)
        {
            var assignedStudentIds = await GetAssignedStudentIdsForLecturerAsync(type, access.CallerUserId);
            submissions = submissions.Where(s => assignedStudentIds.Contains(s.StudentId));
        }

        return await MapToResponsesAsync(submissions);
    }

    public async Task<StudentSubmissionResponse?> GetByIdAsync(SubmissionType type, Guid id, SubmissionAccessContext access)
    {
        var submission = await _unitOfWork.StudentSubmissions.GetByIdAsync(id);
        if (submission == null || submission.Type != type || !await CanAccessAsync(submission, access, type))
            return null;

        return await MapToResponseAsync(submission);
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(SubmissionType type, Guid id, SubmissionAccessContext access)
    {
        var submission = await _unitOfWork.StudentSubmissions.GetByIdAsync(id);
        if (submission == null || submission.Type != type || !await CanAccessAsync(submission, access, type))
            throw new KeyNotFoundException($"Submission with ID {id} not found.");

        if (!_fileStorageService.FileExists(submission.FilePath))
            throw new FileNotFoundException("File not found on disk.");

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(submission.FilePath);

        using var _ = fileStream;
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType, submission.OriginalFileName);
    }

    public async Task<IEnumerable<SubmissionCommentResponse>> GetCommentsAsync(SubmissionType type, Guid submissionId, SubmissionAccessContext access)
    {
        var submission = await _unitOfWork.StudentSubmissions.GetByIdAsync(submissionId);
        if (submission == null || submission.Type != type || !await CanAccessAsync(submission, access, type))
            throw new KeyNotFoundException($"Submission with ID {submissionId} not found.");

        var comments = await _unitOfWork.SubmissionComments.GetBySubmissionIdAsync(submissionId);
        return await MapCommentsToResponsesAsync(comments);
    }

    public async Task<SubmissionCommentResponse> AddCommentAsync(SubmissionType type, Guid submissionId, Guid authorUserId, string text, SubmissionAccessContext access)
    {
        var submission = await _unitOfWork.StudentSubmissions.GetByIdAsync(submissionId);
        if (submission == null || submission.Type != type || !await CanAccessAsync(submission, access, type))
            throw new KeyNotFoundException($"Submission with ID {submissionId} not found.");

        var comment = new SubmissionComment
        {
            Id = Guid.NewGuid(),
            StudentSubmissionId = submissionId,
            AuthorUserId = authorUserId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SubmissionComments.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        await NotifyStudentOfCommentAsync(type, submission);

        return await MapCommentToResponseAsync(comment);
    }

    /// <summary>
    /// Admin can access any submission. Anyone else's access is resolved dynamically: either they
    /// own the submission (student), or they're the Lecturer currently assigned to that student for
    /// this submission type - whichever role they authenticated with doesn't matter, only identity.
    /// </summary>
    private async Task<bool> CanAccessAsync(StudentSubmission submission, SubmissionAccessContext access, SubmissionType type)
    {
        if (access.IsAdmin)
            return true;

        var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentId);
        if (student != null && student.UserId == access.CallerUserId)
            return true;

        return await IsAssignedLecturerAsync(type, submission.StudentId, access.CallerUserId);
    }

    private async Task<bool> IsAssignedLecturerAsync(SubmissionType type, Guid studentId, Guid callerUserId)
    {
        var lecturerIds = await GetAssignedLecturerUserIdsAsync(type, studentId);
        return lecturerIds.Contains(callerUserId);
    }

    /// <summary>
    /// Every Lecturer ever allocated to this student for this submission type (InternshipAllocation
    /// for InternshipReport, DissertationAllocation for Dissertation) - not scoped to a single
    /// academic year, since assignment is treated as a durable relationship for visibility purposes.
    /// </summary>
    private async Task<List<Guid>> GetAssignedLecturerUserIdsAsync(SubmissionType type, Guid studentId)
    {
        if (type == SubmissionType.InternshipReport)
        {
            var allocations = await _unitOfWork.InternshipAllocations.GetAllAsync();
            return allocations.Where(a => a.StudentId == studentId).Select(a => a.LecturerUserId).Distinct().ToList();
        }

        var dissertationAllocations = await _unitOfWork.DissertationAllocations.GetAllAsync();
        return dissertationAllocations.Where(a => a.StudentId == studentId).Select(a => a.LecturerUserId).Distinct().ToList();
    }

    private async Task<List<Guid>> GetAssignedStudentIdsForLecturerAsync(SubmissionType type, Guid lecturerUserId)
    {
        if (type == SubmissionType.InternshipReport)
        {
            var allocations = await _unitOfWork.InternshipAllocations.GetAllAsync();
            return allocations.Where(a => a.LecturerUserId == lecturerUserId).Select(a => a.StudentId).Distinct().ToList();
        }

        var dissertationAllocations = await _unitOfWork.DissertationAllocations.GetAllAsync();
        return dissertationAllocations.Where(a => a.LecturerUserId == lecturerUserId).Select(a => a.StudentId).Distinct().ToList();
    }

    private async Task NotifyAssignedLecturersAsync(SubmissionType type, Student student, StudentSubmission submission)
    {
        var lecturerIds = await GetAssignedLecturerUserIdsAsync(type, student.Id);
        if (!lecturerIds.Any())
            return;

        var studentFullName = $"{student.FirstName} {student.LastName}";
        var typeLabel = TypeLabel(type);

        foreach (var lecturerId in lecturerIds)
        {
            var lecturer = await _unitOfWork.Users.GetByIdAsync(lecturerId);
            if (lecturer == null)
                continue;

            var (subject, html) = EmailTemplates.SubmissionUploaded(lecturer.Username, studentFullName, typeLabel);
            await _emailSender.SendEmailAsync(lecturer.Email, lecturer.Username, subject, html);
        }
    }

    private async Task NotifyStudentOfCommentAsync(SubmissionType type, StudentSubmission submission)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(submission.StudentId);
        if (student == null)
            return;

        var user = await _unitOfWork.Users.GetByIdAsync(student.UserId);
        if (user == null)
            return;

        var studentFullName = $"{student.FirstName} {student.LastName}";
        var (subject, html) = EmailTemplates.SubmissionCommented(studentFullName, TypeLabel(type));
        await _emailSender.SendEmailAsync(user.Email, studentFullName, subject, html);
    }

    private static string TypeLabel(SubmissionType type) =>
        type == SubmissionType.InternshipReport ? "internship report" : "dissertation/project write-up";

    private static void ValidateFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF and DOC/DOCX files are allowed.");

        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > 50 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 50MB limit.");
    }

    private async Task<StudentSubmissionResponse> MapToResponseAsync(StudentSubmission submission, Student? student = null)
    {
        student ??= await _unitOfWork.Students.GetByIdAsync(submission.StudentId);
        var commentCount = (await _unitOfWork.SubmissionComments.GetBySubmissionIdAsync(submission.Id)).Count();
        return MapToResponse(submission, student, commentCount);
    }

    private async Task<IEnumerable<StudentSubmissionResponse>> MapToResponsesAsync(IEnumerable<StudentSubmission> submissions)
    {
        var list = submissions.ToList();
        var students = await _unitOfWork.Students.GetAllAsync();
        var studentsById = students.ToDictionary(s => s.Id, s => s);

        var responses = new List<StudentSubmissionResponse>();
        foreach (var submission in list)
        {
            studentsById.TryGetValue(submission.StudentId, out var student);
            var commentCount = (await _unitOfWork.SubmissionComments.GetBySubmissionIdAsync(submission.Id)).Count();
            responses.Add(MapToResponse(submission, student, commentCount));
        }

        return responses;
    }

    private static StudentSubmissionResponse MapToResponse(StudentSubmission submission, Student? student, int commentCount)
    {
        return new StudentSubmissionResponse
        {
            Id = submission.Id,
            StudentId = submission.StudentId,
            StudentFullName = student == null ? string.Empty : $"{student.FirstName} {student.LastName}",
            StudentIdNumber = student?.StudentId ?? string.Empty,
            ProgramName = student?.ProgramName ?? string.Empty,
            Type = submission.Type,
            TypeText = submission.Type.ToString(),
            FilePath = submission.FilePath,
            OriginalFileName = submission.OriginalFileName,
            SubmissionCount = submission.SubmissionCount,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            CommentCount = commentCount
        };
    }

    private async Task<SubmissionCommentResponse> MapCommentToResponseAsync(SubmissionComment comment)
    {
        var author = await _unitOfWork.Users.GetByIdAsync(comment.AuthorUserId);
        return MapCommentToResponse(comment, author);
    }

    private async Task<IEnumerable<SubmissionCommentResponse>> MapCommentsToResponsesAsync(IEnumerable<SubmissionComment> comments)
    {
        var list = comments.ToList();
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(c =>
        {
            usersById.TryGetValue(c.AuthorUserId, out var author);
            return MapCommentToResponse(c, author);
        });
    }

    private static SubmissionCommentResponse MapCommentToResponse(SubmissionComment comment, User? author)
    {
        return new SubmissionCommentResponse
        {
            Id = comment.Id,
            StudentSubmissionId = comment.StudentSubmissionId,
            AuthorUserId = comment.AuthorUserId,
            AuthorUsername = author?.Username ?? string.Empty,
            AuthorRole = author?.Role.ToString() ?? string.Empty,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };
    }
}
