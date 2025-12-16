using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SkillSync.Core;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Dtos;
using System.IO.Compression;
using System.Linq;

namespace SkillSync.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AttachmentService> _logger;
        private readonly IWebHostEnvironment _environment;

        public AttachmentService(
            AppDbContext context,
            IFileStorageService fileStorage,
            ILogger<AttachmentService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
            _environment = environment;
        }

        public async Task<Result<List<Attachment>>> CreateAsync(
            int designId,
            List<IFormFile> files,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting upload for design {DesignId}", designId);

                var design = await _context.Designs
                    .Include(d => d.Attachments.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(d => d.Id == designId, ct);

                if (design == null)
                {
                    _logger.LogWarning("Design {DesignId} not found", designId);
                    return Result<List<Attachment>>.Failure("Design not found");
                }

                if (files == null || files.Count == 0)
                {
                    return Result<List<Attachment>>.Failure("At least one file is required");
                }

                foreach (var file in files)
                {
                    if (!IsImageFile(file))
                    {
                        return Result<List<Attachment>>.Failure($"File '{file.FileName}' is not a valid image. Allowed types: JPG, PNG, GIF, BMP, WEBP");
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        return Result<List<Attachment>>.Failure($"File '{file.FileName}' exceeds maximum size of 10MB");
                    }
                }

                var newAttachments = new List<Attachment>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    _logger.LogInformation("Processing file: {FileName} ({Size} bytes)",
                        file.FileName, file.Length);

                    var saveResult = await _fileStorage.SaveAsync(file, ct);
                    if (!saveResult.IsSuccess)
                    {
                        await RollbackSavedFiles(newAttachments);

                        var errorMessage = saveResult.Errors?.FirstOrDefault() ?? "Unknown error";
                        return Result<List<Attachment>>.Failure($"Failed to save file '{file.FileName}': {errorMessage}");
                    }

                    var attachment = new Attachment
                    {
                        DesignId = designId,
                        FileName = SanitizeFileName(file.FileName),
                        MimeType = GetMimeType(file.FileName),
                        FileSizeBytes = file.Length,
                        RelativePath = saveResult.Value ?? string.Empty,
                        StorageType = "FileSystem",
                        IsPrimary = (i == 0 && !(design.Attachments?.Any(a => a.IsPrimary && a.IsActive) ?? false)),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    newAttachments.Add(attachment);
                }

                await _context.Attachments.AddRangeAsync(newAttachments, ct);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Successfully uploaded {Count} files for design {DesignId}",
                    newAttachments.Count, designId);

                return Result<List<Attachment>>.Success(newAttachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachments for design {DesignId}", designId);
                return Result<List<Attachment>>.Failure($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result> RemoveAsync(
            int attachmentId,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Deleting attachment {AttachmentId}", attachmentId);

                var attachment = await _context.Attachments
                    .Include(a => a.Design)
                        .ThenInclude(d => d.Attachments.Where(a => a.IsActive))
                    .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

                if (attachment == null || !attachment.IsActive)
                {
                    _logger.LogWarning("Attachment {AttachmentId} not found or already deleted", attachmentId);
                    return Result.Failure("Attachment not found");
                }

                if (attachment.Design == null)
                {
                    return Result.Failure("Associated design not found");
                }

                var activeAttachmentsCount = attachment.Design.Attachments
                    .Count(a => a.IsActive && a.Id != attachmentId);

                if (activeAttachmentsCount == 0)
                {
                    return Result.Failure("Cannot delete the last attachment of a design");
                }

                if (attachment.IsPrimary && activeAttachmentsCount > 0)
                {
                    var nextPrimary = attachment.Design.Attachments
                        .FirstOrDefault(a => a.IsActive && a.Id != attachmentId);

                    if (nextPrimary != null)
                    {
                        nextPrimary.IsPrimary = true;
                        _context.Attachments.Update(nextPrimary);
                    }
                }

                attachment.IsActive = false;
                _context.Attachments.Update(attachment);

                if (!string.IsNullOrEmpty(attachment.RelativePath))
                {
                    var deleteResult = await _fileStorage.DeleteAsync(attachment.RelativePath);
                    if (!deleteResult.IsSuccess)
                    {
                        var errorMessage = deleteResult.Errors?.FirstOrDefault() ?? "Unknown error";
                        _logger.LogWarning("Failed to delete physical file for attachment {AttachmentId}: {Error}",
                            attachmentId, errorMessage);
                    }
                }

                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Successfully deleted attachment {AttachmentId}", attachmentId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return Result.Failure($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result<Attachment>> GetAsync(
            int attachmentId,
            CancellationToken ct)
        {
            try
            {
                var attachment = await _context.Attachments
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.IsActive, ct);

                if (attachment == null)
                {
                    return Result<Attachment>.Failure("Attachment not found or inactive");
                }

                return Result<Attachment>.Success(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachment {AttachmentId}", attachmentId);
                return Result<Attachment>.Failure($"An error occurred: {ex.Message}");
            }
        }

        private bool IsImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
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

        private string SanitizeFileName(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                safeFileName = safeFileName.Replace(c.ToString(), "_");
            }

            return safeFileName;
        }

        private async Task RollbackSavedFiles(List<Attachment> attachments)
        {
            foreach (var attachment in attachments)
            {
                if (!string.IsNullOrEmpty(attachment.RelativePath))
                {
                    await _fileStorage.DeleteAsync(attachment.RelativePath);
                }
            }
        }


        public async Task<Result<List<AttachmentDto>>> GetDesignAttachmentsAsync(
    int designId,
    CancellationToken ct)
        {
            try
            {
                var attachments = await _context.Attachments
                    .Where(a => a.DesignId == designId && a.IsActive)
                    .Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        MimeType = a.MimeType,
                        FileSizeBytes = a.FileSizeBytes,
                        IsPrimary = a.IsPrimary,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync(ct);

                return Result<List<AttachmentDto>>.Success(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting design attachments for design {DesignId}", designId);
                return Result<List<AttachmentDto>>.Failure($"An error occurred: {ex.Message}");
            }
        }

        public async Task<Result<ZipFileDto>> DownloadAllAttachmentsAsync(
            int designId,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Creating ZIP for design {DesignId}", designId);

                // 1. جلب المرفقات
                var attachments = await _context.Attachments
                    .Where(a => a.DesignId == designId && a.IsActive)
                    .ToListAsync(ct);

                if (!attachments.Any())
                    return Result<ZipFileDto>.Failure("No attachments found for this design");

                // 2. إنشاء ZIP
                var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var attachment in attachments)
                    {
                        if (string.IsNullOrEmpty(attachment.RelativePath))
                            continue;

                        var filePath = Path.Combine(_environment.WebRootPath, attachment.RelativePath);
                        if (!System.IO.File.Exists(filePath))
                        {
                            _logger.LogWarning("File not found: {Path}", filePath);
                            continue;
                        }

                        try
                        {
                            var entry = archive.CreateEntry(attachment.FileName, CompressionLevel.Fastest);
                            using var entryStream = entry.Open();
                            using var fileStream = System.IO.File.OpenRead(filePath);
                            await fileStream.CopyToAsync(entryStream);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to add file to ZIP: {FileName}", attachment.FileName);
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                // 3. إرجاع الـ DTO
                return Result<ZipFileDto>.Success(new ZipFileDto
                {
                    Content = memoryStream,
                    FileName = $"design_{designId}_attachments.zip"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZIP for design {DesignId}", designId);
                return Result<ZipFileDto>.Failure($"Error creating ZIP file: {ex.Message}");
            }
        }
    }
}