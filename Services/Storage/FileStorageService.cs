namespace AspnetCoreMvcFull.Services
{
  public class LocalFileStorageService : IFileStorageService
  {
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
      _env = env;
      _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string containerName)
    {
      if (file == null) return string.Empty;

      var extension = Path.GetExtension(file.FileName);
      var fileName = $"{Guid.NewGuid()}{extension}";
      var folder = Path.Combine(_env.WebRootPath, containerName);

      if (!Directory.Exists(folder))
      {
        Directory.CreateDirectory(folder);
      }

      var filePath = Path.Combine(folder, fileName);
      using (var memoryStream = new MemoryStream())
      {
        await file.CopyToAsync(memoryStream);
        await File.WriteAllBytesAsync(filePath, memoryStream.ToArray());
      }

      // Pastikan HttpContext tidak null
      if (_httpContextAccessor.HttpContext != null)
      {
        var currentUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
        var pathForDb = Path.Combine(currentUrl, containerName, fileName).Replace("\\", "/");
        return pathForDb;
      }
      else
      {
        // Fallback jika HttpContext null
        var pathForDb = $"/{containerName}/{fileName}";
        return pathForDb;
      }
    }

    public Task DeleteFileAsync(string filePath, string containerName)
    {
      if (string.IsNullOrEmpty(filePath)) return Task.CompletedTask;

      var fileName = Path.GetFileName(filePath);
      var fileDirectory = Path.Combine(_env.WebRootPath, containerName, fileName);

      if (File.Exists(fileDirectory))
      {
        File.Delete(fileDirectory);
      }

      return Task.CompletedTask;
    }
  }
}
