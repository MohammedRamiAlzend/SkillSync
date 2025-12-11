using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.Core;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Services;
using System.IO.Compression;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AttachmentsController> _logger;
        private readonly AppDbContext _context;  
        private readonly IWebHostEnvironment _environment;  

        public AttachmentsController(
            IAttachmentService attachmentService,
            IFileStorageService fileStorage,
            ILogger<AttachmentsController> logger,
            AppDbContext context,  
            IWebHostEnvironment environment)  
        {
            _attachmentService = attachmentService;
            _fileStorage = fileStorage;
            _logger = logger;
            _context = context;  
            _environment = environment;  
        }

        [HttpPost("designs/{designId}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachments(
            [FromRoute] int designId,
            CancellationToken ct)
        {
            _logger.LogInformation("Upload request for design {DesignId}", designId);

            try
            {
                var httpRequest = HttpContext.Request;
                var files = httpRequest.Form.Files;

                _logger.LogInformation("Received {Count} files", files?.Count ?? 0);

                if (files == null || files.Count == 0)
                {
                    _logger.LogWarning("No files received in request");
                    return BadRequest(new { error = "At least one file is required" });
                }

                var fileList = files.ToList();
                var result = await _attachmentService.CreateAsync(designId, fileList, ct);

                if (!result.IsSuccess)
                {
                    var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                    _logger.LogWarning("Upload failed for design {DesignId}: {Error}",
                        designId, errorMessage);
                    return BadRequest(new { error = errorMessage });
                }

                _logger.LogInformation("Upload successful for design {DesignId}", designId);

                var attachmentsList = result.Value ?? new List<Attachment>();

                return Ok(new
                {
                    message = "Files uploaded successfully",
                    count = attachmentsList.Count,
                    attachments = attachmentsList.Select(a => new {
                        a.Id,
                        a.FileName,
                        a.FileSizeBytes,
                        a.MimeType,
                        a.IsPrimary,
                        a.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadAttachments for design {DesignId}", designId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(
            [FromRoute] int id,
            CancellationToken ct)
        {
            _logger.LogInformation("Delete request for attachment {AttachmentId}", id);

            var result = await _attachmentService.RemoveAsync(id, ct);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                _logger.LogWarning("Delete failed for attachment {AttachmentId}: {Error}",
                    id, errorMessage);
                return BadRequest(new { error = errorMessage });
            }

            _logger.LogInformation("Delete successful for attachment {AttachmentId}", id);
            return Ok(new { message = "Attachment deleted successfully" });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadAttachment(
            [FromRoute] int id,
            CancellationToken ct)
        {
            _logger.LogInformation("Download request for attachment {AttachmentId}", id);

            var attachmentResult = await _attachmentService.GetAsync(id, ct);
            if (!attachmentResult.IsSuccess)
            {
                var errorMessage = attachmentResult.Errors?.FirstOrDefault() ?? "Unknown error";
                _logger.LogWarning("Attachment {AttachmentId} not found: {Error}",
                    id, errorMessage);
                return NotFound(new { error = errorMessage });
            }

            var attachment = attachmentResult.Value;

            if (attachment == null || string.IsNullOrEmpty(attachment.RelativePath))
            {
                _logger.LogWarning("Attachment {AttachmentId} has invalid data", id);
                return NotFound(new { error = "Attachment data is incomplete" });
            }

            var fileResult = await _fileStorage.GetAsync(attachment.RelativePath);
            if (!fileResult.IsSuccess)
            {
                _logger.LogError("File not found for attachment {AttachmentId}: {Path}",
                    id, attachment.RelativePath);
                return NotFound(new { error = "File not found on disk" });
            }

            var (stream, fileName, mimeType) = fileResult.Value;

            _logger.LogInformation("Download successful for attachment {AttachmentId}", id);

            return File(stream, mimeType, fileName);
        }

        [HttpGet("designs/{designId}/attachments")]
        public async Task<IActionResult> GetDesignAttachments(
            [FromRoute] int designId,
            CancellationToken ct)
        {
            var attachments = await _context.Attachments
                .Where(a => a.DesignId == designId && a.IsActive)
                .Select(a => new {
                    a.Id,
                    a.FileName,
                    a.FileSizeBytes,
                    a.MimeType,
                    a.IsPrimary,
                    a.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(new
            {
                designId,
                count = attachments.Count,
                attachments
            });
        }

        [HttpGet("designs/{designId}/attachments/download")]
        public async Task<IActionResult> DownloadAllAttachments(
        [FromRoute] int designId,
        CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("ZIP download request for design {DesignId}", designId);

                var attachments = await _context.Attachments
                    .Where(a => a.DesignId == designId && a.IsActive)
                    .ToListAsync(ct);

                if (!attachments.Any())
                    return NotFound(new { error = "No attachments found for this design" });

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

                var result = new FileStreamResult(memoryStream, "application/zip")
                {
                    FileDownloadName = $"design_{designId}_attachments.zip"
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZIP for design {DesignId}", designId);
                return StatusCode(500, new { error = $"Error creating ZIP file: {ex.Message}" });
            }
        }
    }
}