using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public AssignmentService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<AssignmentResponse> CreateAsync(AssignmentRequest request, IFormFile? file)
    {
        string? filePath = null;
        string? originalFileName = null;

        if (file != null && file.Length > 0)
        {
            ValidateFile(file);
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            filePath = await _fileStorageService.SaveFileAsync(stream, fileName, "assignments");
            originalFileName = file.FileName;
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseName = request.CourseName,
            CourseCode = request.CourseCode,
            AssignmentTitle = request.AssignmentTitle,
            Importance = request.Importance,
            DateCreated = DateTime.UtcNow,
            DueDate = request.DueDate,
            FilePath = filePath,
            OriginalFileName = originalFileName
        };

        await _unitOfWork.Assignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(assignment);
    }

    public async Task<AssignmentResponse?> GetByIdAsync(Guid id)
    {
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
        return assignment == null ? null : MapToResponse(assignment);
    }

    public async Task<IEnumerable<AssignmentResponse>> GetAllAsync()
    {
        var assignments = await _unitOfWork.Assignments.GetAllAsync();
        return assignments.Select(MapToResponse);
    }

    public async Task<PagedResponse<AssignmentResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.Assignments.GetPagedAsync(pageNumber, pageSize);

        return new PagedResponse<AssignmentResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AssignmentResponse> UpdateAsync(Guid id, AssignmentRequest request, IFormFile? file)
    {
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Assignment with ID {id} not found.");

        assignment.CourseName = request.CourseName;
        assignment.CourseCode = request.CourseCode;
        assignment.AssignmentTitle = request.AssignmentTitle;
        assignment.Importance = request.Importance;
        assignment.DueDate = request.DueDate;

        if (file != null && file.Length > 0)
        {
            ValidateFile(file);

            if (!string.IsNullOrEmpty(assignment.FilePath))
                await _fileStorageService.DeleteFileAsync(assignment.FilePath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            assignment.FilePath = await _fileStorageService.SaveFileAsync(stream, fileName, "assignments");
            assignment.OriginalFileName = file.FileName;
        }

        await _unitOfWork.Assignments.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(assignment);
    }

    public async Task DeleteAsync(Guid id)
    {
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Assignment with ID {id} not found.");

        if (!string.IsNullOrEmpty(assignment.FilePath))
            await _fileStorageService.DeleteFileAsync(assignment.FilePath);

        await _unitOfWork.Assignments.DeleteAsync(assignment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id)
    {
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Assignment with ID {id} not found.");

        if (string.IsNullOrEmpty(assignment.FilePath))
            throw new FileNotFoundException("No file attached to this assignment.");

        if (!_fileStorageService.FileExists(assignment.FilePath))
            throw new FileNotFoundException("File not found on disk.");

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(assignment.FilePath);

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType, assignment.OriginalFileName ?? "assignment_file");
    }

    private static void ValidateFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt", ".zip" };

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF, DOCX, DOC, TXT, and ZIP files are allowed for assignments.");

        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > 20 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 20MB limit.");
    }

    private static AssignmentResponse MapToResponse(Assignment assignment)
    {
        return new AssignmentResponse
        {
            Id = assignment.Id,
            CourseName = assignment.CourseName,
            CourseCode = assignment.CourseCode,
            AssignmentTitle = assignment.AssignmentTitle,
            Importance = assignment.Importance,
            ImportanceText = assignment.Importance.ToString(),
            DateCreated = assignment.DateCreated,
            DueDate = assignment.DueDate,
            FilePath = assignment.FilePath,
            OriginalFileName = assignment.OriginalFileName
        };
    }
}
