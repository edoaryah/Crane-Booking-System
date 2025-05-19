public interface IFileStorageService
{
  Task<string> SaveFileAsync(IFormFile file, string containerName);
  Task DeleteFileAsync(string filePath, string containerName);
}
