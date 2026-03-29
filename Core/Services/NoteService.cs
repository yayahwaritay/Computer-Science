using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Services;

public class NoteService : INoteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public NoteService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<NoteResponse> CreateAsync(NoteRequest request, IFormFile file)
    {
        ValidateFile(file);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        string filePath;

        using (var stream = file.OpenReadStream())
        {
            filePath = await _fileStorageService.SaveFileAsync(stream, fileName, "notes");
        }

        var note = new Note
        {
            Id = Guid.NewGuid(),
            CourseName = request.CourseName,
            CourseCode = request.CourseCode,
            FilePath = filePath,
            OriginalFileName = file.FileName,
            UploadDate = DateTime.UtcNow
        };

        await _unitOfWork.Notes.AddAsync(note);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(note);
    }

    public async Task<NoteResponse?> GetByIdAsync(Guid id)
    {
        var note = await _unitOfWork.Notes.GetByIdAsync(id);
        return note == null ? null : MapToResponse(note);
    }

    public async Task<IEnumerable<NoteResponse>> GetAllAsync()
    {
        var notes = await _unitOfWork.Notes.GetAllAsync();
        return notes.Select(MapToResponse);
    }

    public async Task<PagedResponse<NoteResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.Notes.GetPagedAsync(pageNumber, pageSize);

        return new PagedResponse<NoteResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<NoteResponse> UpdateAsync(Guid id, NoteRequest request, IFormFile? file)
    {
        var note = await _unitOfWork.Notes.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Note with ID {id} not found.");

        note.CourseName = request.CourseName;
        note.CourseCode = request.CourseCode;

        if (file != null)
        {
            ValidateFile(file);

            await _fileStorageService.DeleteFileAsync(note.FilePath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            note.FilePath = await _fileStorageService.SaveFileAsync(stream, fileName, "notes");
            note.OriginalFileName = file.FileName;
        }

        await _unitOfWork.Notes.UpdateAsync(note);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(note);
    }

    public async Task DeleteAsync(Guid id)
    {
        var note = await _unitOfWork.Notes.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Note with ID {id} not found.");

        await _fileStorageService.DeleteFileAsync(note.FilePath);
        await _unitOfWork.Notes.DeleteAsync(note);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id)
    {
        var note = await _unitOfWork.Notes.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Note with ID {id} not found.");

        if (!_fileStorageService.FileExists(note.FilePath))
            throw new FileNotFoundException("File not found on disk.");

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(note.FilePath);

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType, note.OriginalFileName);
    }

    private static void ValidateFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF and DOCX files are allowed for notes.");

        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > 20 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 20MB limit.");
    }

    private static NoteResponse MapToResponse(Note note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            CourseName = note.CourseName,
            CourseCode = note.CourseCode,
            FilePath = note.FilePath,
            OriginalFileName = note.OriginalFileName,
            UploadDate = note.UploadDate
        };
    }
}
