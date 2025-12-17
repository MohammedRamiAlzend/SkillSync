using Microsoft.AspNetCore.Hosting;
using SkillSync.Core;

namespace SkillSync.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _uploadsPath;

        public FileStorageService(
            IWebHostEnvironment environment,
            ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;

            // تحديد مسار التحميلات
            _uploadsPath = Path.Combine(_environment.WebRootPath, "Uploads");

            // إنشاء المجلد إذا لم يكن موجوداً
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
                _logger.LogInformation("Created uploads directory at: {Path}", _uploadsPath);
            }
        }

        public async Task<Result<string>> SaveAsync(IFormFile file, CancellationToken ct)
        {
            try
            {
                // 1. إنشاء اسم فريد للملف
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}{extension}";

                // 2. تحديد المسار النسبي
                var relativePath = Path.Combine("Uploads", uniqueFileName);

                // 3. المسار الكامل
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                // 4. حفظ الملف
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, ct);
                }

                _logger.LogInformation("File saved: {FileName} -> {Path}",
                    file.FileName, relativePath);

                return Result<string>.Success(relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                return Result<string>.Failure($"Failed to save file: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    return Result.Failure("Invalid file path");
                }

                // 1. بناء المسار الكامل
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                // 2. التحقق من وجود الملف
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found for deletion: {Path}", fullPath);
                    return Result.Success(); // نجاح لأنه ما في ملف نحذفه
                }

                // 3. حذف الملف
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {Path}", fullPath);

                await Task.CompletedTask;
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", relativePath);
                return Result.Failure($"Failed to delete file: {ex.Message}");
            }
        }

        public async Task<Result<(Stream Stream, string FileName, string MimeType)>> GetAsync(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    return Result<(Stream, string, string)>.Failure("Invalid file path");
                }

                // 1. بناء المسار الكامل
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                // 2. التحقق من وجود الملف
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {Path}", fullPath);
                    return Result<(Stream, string, string)>.Failure("File not found");
                }

                // 3. فتح stream للملف
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                // 4. استخراج اسم الملف
                var fileName = Path.GetFileName(relativePath);

                // 5. تحديد MIME type
                var mimeType = GetMimeType(fileName);

                _logger.LogDebug("File retrieved: {FileName} ({MimeType})", fileName, mimeType);

                await Task.CompletedTask;
                return Result<(Stream, string, string)>.Success((stream, fileName, mimeType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file: {Path}", relativePath);
                return Result<(Stream, string, string)>.Failure($"Failed to retrieve file: {ex.Message}");
            }
        }

        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}