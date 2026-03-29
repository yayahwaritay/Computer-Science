using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Services;

public class PastQuestionService : IPastQuestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public PastQuestionService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<PastQuestionResponse> CreateAsync(PastQuestionRequest request, IFormFile file)
    {
        ValidatePdfFile(file);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        string filePath;

        using (var stream = file.OpenReadStream())
        {
            filePath = await _fileStorageService.SaveFileAsync(stream, fileName, "pastquestions");
        }

        var pastQuestion = new PastQuestion
        {
            Id = Guid.NewGuid(),
            CourseName = request.CourseName,
            CourseCode = request.CourseCode,
            FilePath = filePath,
            OriginalFileName = file.FileName,
            UploadDate = DateTime.UtcNow
        };

        await _unitOfWork.PastQuestions.AddAsync(pastQuestion);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(pastQuestion);
    }

    public async Task<PastQuestionResponse?> GetByIdAsync(Guid id)
    {
        var pastQuestion = await _unitOfWork.PastQuestions.GetByIdAsync(id);
        return pastQuestion == null ? null : MapToResponse(pastQuestion);
    }

    public async Task<IEnumerable<PastQuestionResponse>> GetAllAsync()
    {
        var pastQuestions = await _unitOfWork.PastQuestions.GetAllAsync();
        return pastQuestions.Select(MapToResponse);
    }

    public async Task<PagedResponse<PastQuestionResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.PastQuestions.GetPagedAsync(pageNumber, pageSize);

        return new PagedResponse<PastQuestionResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PastQuestionResponse> UpdateAsync(Guid id, PastQuestionRequest request, IFormFile? file)
    {
        var pastQuestion = await _unitOfWork.PastQuestions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Past question with ID {id} not found.");

        pastQuestion.CourseName = request.CourseName;
        pastQuestion.CourseCode = request.CourseCode;

        if (file != null)
        {
            ValidatePdfFile(file);

            _fileStorageService.DeleteFileAsync(pastQuestion.FilePath).Wait();

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            pastQuestion.FilePath = await _fileStorageService.SaveFileAsync(stream, fileName, "pastquestions");
            pastQuestion.OriginalFileName = file.FileName;
        }

        await _unitOfWork.PastQuestions.UpdateAsync(pastQuestion);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(pastQuestion);
    }

    public async Task DeleteAsync(Guid id)
    {
        var pastQuestion = await _unitOfWork.PastQuestions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Past question with ID {id} not found.");

        await _fileStorageService.DeleteFileAsync(pastQuestion.FilePath);
        await _unitOfWork.PastQuestions.DeleteAsync(pastQuestion);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id)
    {
        var pastQuestion = await _unitOfWork.PastQuestions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Past question with ID {id} not found.");

        if (!_fileStorageService.FileExists(pastQuestion.FilePath))
            throw new FileNotFoundException("File not found on disk.");

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(pastQuestion.FilePath);

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType, pastQuestion.OriginalFileName);
    }

    private static void ValidatePdfFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf")
            throw new InvalidOperationException("Only PDF files are allowed for past questions.");

        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (file.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 10MB limit.");
    }

    private static PastQuestionResponse MapToResponse(PastQuestion pastQuestion)
    {
        return new PastQuestionResponse
        {
            Id = pastQuestion.Id,
            CourseName = pastQuestion.CourseName,
            CourseCode = pastQuestion.CourseCode,
            FilePath = pastQuestion.FilePath,
            OriginalFileName = pastQuestion.OriginalFileName,
            UploadDate = pastQuestion.UploadDate
        };
    }
}
