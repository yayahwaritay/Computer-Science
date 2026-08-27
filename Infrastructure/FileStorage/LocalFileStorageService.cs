using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;

namespace CompSci.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _basePath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _webRootPath = Path.Combine(env.ContentRootPath, "wwwroot");
        _basePath = Path.Combine(_webRootPath, "uploads");
        _contentTypeProvider = new FileExtensionContentTypeProvider();

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder)
    {
        var folderPath = Path.Combine(_basePath, folder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        using var fileStreamOut = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOut);

        return Path.Combine("uploads", folder, fileName);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        var combinedPath = ResolvePath(filePath);

        if (File.Exists(combinedPath))
        {
            File.Delete(combinedPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<(Stream FileStream, string ContentType)> GetFileAsync(string filePath)
    {
        var combinedPath = ResolvePath(filePath);

        if (!File.Exists(combinedPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        _contentTypeProvider.TryGetContentType(combinedPath, out var contentType);
        contentType ??= "application/octet-stream";

        var fileStream = new FileStream(combinedPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(((Stream)fileStream, contentType));
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(ResolvePath(filePath));
    }

    private string ResolvePath(string filePath)
    {
        return Path.Combine(_webRootPath, filePath.Replace("\\", "/"));
    }
}
