using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Export;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Services;

public class DissertationService : IDissertationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDissertationPdfBuilder _pdfBuilder;

    public DissertationService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, IDissertationPdfBuilder pdfBuilder)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _pdfBuilder = pdfBuilder;
    }

    public async Task<DissertationResponse> CreateAsync(DissertationRequest request, IFormFile file, Guid createdByUserId)
    {
        ValidateFile(file);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        string filePath;

        using (var stream = file.OpenReadStream())
        {
            filePath = await _fileStorageService.SaveFileAsync(stream, fileName, "dissertations");
        }

        var dissertation = new Dissertation
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            StudentName = request.StudentName,
            StudentId = request.StudentId,
            Program = request.Program,
            Department = request.Department,
            School = request.School,
            Topic = request.Topic,
            AcademicYear = request.AcademicYear,
            Grade = request.Grade,
            FilePath = filePath,
            OriginalFileName = file.FileName,
            UploadDate = DateTime.UtcNow
        };

        await _unitOfWork.Dissertations.AddAsync(dissertation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(dissertation);
    }

    public async Task<DissertationResponse?> GetByIdAsync(Guid id, DissertationAccessContext access)
    {
        var dissertation = await _unitOfWork.Dissertations.GetByIdAsync(id);
        if (dissertation == null || !CanAccess(dissertation, access))
            return null;

        return await MapToResponseAsync(dissertation);
    }

    public async Task<IEnumerable<DissertationResponse>> GetAllAsync(DissertationAccessContext access)
    {
        var dissertations = access.IsAdmin
            ? await _unitOfWork.Dissertations.GetAllAsync()
            : await _unitOfWork.Dissertations.GetByCreatorAsync(access.UserId);

        return await MapToResponsesAsync(dissertations);
    }

    public async Task<PagedResponse<DissertationResponse>> GetPagedAsync(int pageNumber, int pageSize, DissertationAccessContext access)
    {
        var (data, totalCount) = access.IsAdmin
            ? await _unitOfWork.Dissertations.GetPagedAsync(pageNumber, pageSize)
            : await _unitOfWork.Dissertations.GetPagedByCreatorAsync(access.UserId, pageNumber, pageSize);

        return new PagedResponse<DissertationResponse>
        {
            Data = (await MapToResponsesAsync(data)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<IEnumerable<DissertationResponse>> GetByStudentIdAsync(string studentId, DissertationAccessContext access)
    {
        var dissertations = await _unitOfWork.Dissertations.GetByStudentIdAsync(studentId);
        if (!access.IsAdmin)
            dissertations = dissertations.Where(d => d.CreatedByUserId == access.UserId);

        return await MapToResponsesAsync(dissertations);
    }

    public async Task<DissertationResponse> UpdateAsync(Guid id, DissertationRequest request, IFormFile? file, DissertationAccessContext access)
    {
        var dissertation = await _unitOfWork.Dissertations.GetByIdAsync(id);
        if (dissertation == null || !CanAccess(dissertation, access))
            throw new KeyNotFoundException($"Dissertation with ID {id} not found.");

        dissertation.StudentName = request.StudentName;
        dissertation.StudentId = request.StudentId;
        dissertation.Program = request.Program;
        dissertation.Department = request.Department;
        dissertation.School = request.School;
        dissertation.Topic = request.Topic;
        dissertation.AcademicYear = request.AcademicYear;
        dissertation.Grade = request.Grade;
        dissertation.UpdatedAt = DateTime.UtcNow;

        if (file != null)
        {
            ValidateFile(file);

            await _fileStorageService.DeleteFileAsync(dissertation.FilePath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            dissertation.FilePath = await _fileStorageService.SaveFileAsync(stream, fileName, "dissertations");
            dissertation.OriginalFileName = file.FileName;
        }

        await _unitOfWork.Dissertations.UpdateAsync(dissertation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(dissertation);
    }

    public async Task DeleteAsync(Guid id, DissertationAccessContext access)
    {
        var dissertation = await _unitOfWork.Dissertations.GetByIdAsync(id);
        if (dissertation == null || !CanAccess(dissertation, access))
            throw new KeyNotFoundException($"Dissertation with ID {id} not found.");

        await _fileStorageService.DeleteFileAsync(dissertation.FilePath);
        await _unitOfWork.Dissertations.DeleteAsync(dissertation);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id, DissertationAccessContext access)
    {
        var dissertation = await _unitOfWork.Dissertations.GetByIdAsync(id);
        if (dissertation == null || !CanAccess(dissertation, access))
            throw new KeyNotFoundException($"Dissertation with ID {id} not found.");

        if (!_fileStorageService.FileExists(dissertation.FilePath))
            throw new FileNotFoundException("File not found on disk.");

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(dissertation.FilePath);

        using var _ = fileStream;
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType, dissertation.OriginalFileName);
    }

    public async Task<IEnumerable<DissertationResponse>> SearchAsync(DissertationFilter filter)
    {
        var dissertations = await FilterAsync(filter);
        return await MapToResponsesAsync(dissertations);
    }

    public async Task<byte[]> ExportCsvAsync(DissertationFilter filter)
    {
        var rows = await GetExportRowsAsync(filter);
        return DissertationCsvBuilder.Build(rows);
    }

    public async Task<byte[]> ExportPdfAsync(DissertationFilter filter)
    {
        var rows = await GetExportRowsAsync(filter);
        return _pdfBuilder.Build(rows);
    }

    private async Task<IEnumerable<DissertationExportRow>> GetExportRowsAsync(DissertationFilter filter)
    {
        var dissertations = await FilterAsync(filter);
        return dissertations.Select(d => new DissertationExportRow
        {
            StudentName = d.StudentName,
            StudentId = d.StudentId,
            Program = d.Program,
            Topic = d.Topic,
            AcademicYear = d.AcademicYear
        });
    }

    /// <summary>
    /// Applies the Admin-only cross-cutting filter (academic year range / program / department /
    /// school) across every dissertation record, regardless of who created it.
    /// </summary>
    private async Task<IEnumerable<Dissertation>> FilterAsync(DissertationFilter filter)
    {
        var dissertations = await _unitOfWork.Dissertations.GetAllAsync();

        return dissertations.Where(d =>
            MatchesText(d.Program, filter.Program) &&
            MatchesText(d.Department, filter.Department) &&
            MatchesText(d.School, filter.School) &&
            MatchesYearRange(d.AcademicYear, filter.FromYear, filter.ToYear));
    }

    private static bool MatchesText(string value, string? filterValue)
    {
        return string.IsNullOrWhiteSpace(filterValue)
            || value.Contains(filterValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesYearRange(string academicYear, int? fromYear, int? toYear)
    {
        if (fromYear == null && toYear == null)
            return true;

        // AcademicYear is stored like "2025/2026" — compare against the leading year.
        var leading = academicYear.Length >= 4 ? academicYear[..4] : academicYear;
        if (!int.TryParse(leading, out var year))
            return false;

        if (fromYear.HasValue && year < fromYear.Value)
            return false;

        if (toYear.HasValue && year > toYear.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Admins can access every record; Lecturers are scoped to only the records they created.
    /// </summary>
    private static bool CanAccess(Dissertation dissertation, DissertationAccessContext access)
    {
        return access.IsAdmin || dissertation.CreatedByUserId == access.UserId;
    }

    private static void ValidateFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF and DOC/DOCX files are allowed for dissertation documentation.");

        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > 50 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 50MB limit.");
    }

    private async Task<DissertationResponse> MapToResponseAsync(Dissertation dissertation)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(dissertation.CreatedByUserId);
        return MapToResponse(dissertation, creator?.Username ?? string.Empty, creator?.Email ?? string.Empty);
    }

    private async Task<IEnumerable<DissertationResponse>> MapToResponsesAsync(IEnumerable<Dissertation> dissertations)
    {
        var list = dissertations.ToList();
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(d =>
        {
            usersById.TryGetValue(d.CreatedByUserId, out var creator);
            return MapToResponse(d, creator?.Username ?? string.Empty, creator?.Email ?? string.Empty);
        });
    }

    private static DissertationResponse MapToResponse(Dissertation dissertation, string createdByUsername, string createdByEmail)
    {
        return new DissertationResponse
        {
            Id = dissertation.Id,
            StudentName = dissertation.StudentName,
            StudentId = dissertation.StudentId,
            Program = dissertation.Program,
            Department = dissertation.Department,
            School = dissertation.School,
            Topic = dissertation.Topic,
            AcademicYear = dissertation.AcademicYear,
            Grade = dissertation.Grade,
            FilePath = dissertation.FilePath,
            OriginalFileName = dissertation.OriginalFileName,
            UploadDate = dissertation.UploadDate,
            UpdatedAt = dissertation.UpdatedAt,
            CreatedByUserId = dissertation.CreatedByUserId,
            CreatedByUsername = createdByUsername,
            CreatedByEmail = createdByEmail
        };
    }
}
