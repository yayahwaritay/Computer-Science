namespace CompSci.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder);
    Task<bool> DeleteFileAsync(string filePath);
    Task<(Stream FileStream, string ContentType)> GetFileAsync(string filePath);
    bool FileExists(string filePath);
}
