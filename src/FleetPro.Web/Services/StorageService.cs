namespace FleetPro.Services;

/// <summary>
/// Storage configuration from appsettings.json
/// </summary>
public class StorageSettings
{
    public string UploadPath { get; set; } = "wwwroot/uploads";
    public string ReceiptsFolder { get; set; } = "receipts";
    public string DocumentsFolder { get; set; } = "documents";
    public string ProfileImagesFolder { get; set; } = "profiles";
    public int MaxFileSizeMB { get; set; } = 10;
    public string AllowedExtensions { get; set; } = ".pdf,.jpg,.jpeg,.png,.gif,.doc,.docx,.xls,.xlsx";

    public string[] GetAllowedExtensionsArray() => 
        AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;
}

/// <summary>
/// Service for handling file storage operations
/// </summary>
public interface IStorageService
{
    string GetReceiptsPath();
    string GetDocumentsPath();
    string GetProfileImagesPath();
    Task<string> SaveFileAsync(IFormFile file, string folder, string? customFileName = null);
    bool DeleteFile(string relativePath);
    bool IsAllowedExtension(string fileName);
    string GetRelativeUrl(string folder, string fileName);
}

public class StorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly IWebHostEnvironment _env;

    public StorageService(StorageSettings settings, IWebHostEnvironment env)
    {
        _settings = settings;
        _env = env;
    }

    private string GetBasePath()
    {
        // If UploadPath starts with wwwroot, use WebRootPath
        if (_settings.UploadPath.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
        {
            var subPath = _settings.UploadPath.Replace("wwwroot/", "", StringComparison.OrdinalIgnoreCase)
                                              .Replace("wwwroot\\", "", StringComparison.OrdinalIgnoreCase);
            return Path.Combine(_env.WebRootPath, subPath);
        }
        // Otherwise use absolute path
        return _settings.UploadPath;
    }

    public string GetReceiptsPath() => Path.Combine(GetBasePath(), _settings.ReceiptsFolder);
    public string GetDocumentsPath() => Path.Combine(GetBasePath(), _settings.DocumentsFolder);
    public string GetProfileImagesPath() => Path.Combine(GetBasePath(), _settings.ProfileImagesFolder);

    public async Task<string> SaveFileAsync(IFormFile file, string folder, string? customFileName = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds maximum allowed size of {_settings.MaxFileSizeMB}MB");

        if (!IsAllowedExtension(file.FileName))
            throw new ArgumentException($"File type not allowed. Allowed types: {_settings.AllowedExtensions}");

        var folderPath = folder.ToLower() switch
        {
            "receipts" => GetReceiptsPath(),
            "documents" => GetDocumentsPath(),
            "profiles" => GetProfileImagesPath(),
            _ => Path.Combine(GetBasePath(), folder)
        };

        Directory.CreateDirectory(folderPath);

        var fileName = customFileName ?? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return GetRelativeUrl(folder, fileName);
    }

    public bool DeleteFile(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return false;

        var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return true;
        }
        return false;
    }

    public bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _settings.GetAllowedExtensionsArray().Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public string GetRelativeUrl(string folder, string fileName)
    {
        // Returns URL path like /uploads/receipts/filename.pdf
        var basePath = _settings.UploadPath.Replace("wwwroot", "", StringComparison.OrdinalIgnoreCase)
                                           .Replace("\\", "/");
        if (!basePath.StartsWith("/")) basePath = "/" + basePath;
        return $"{basePath}/{folder}/{fileName}".Replace("//", "/");
    }
}
