using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSync.Core;
using SkillSync.Services;
using SkillSync.Data.Entities;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class AttachmentsController(
        IAttachmentService attachmentService,
        IFileStorageService fileStorage,
        ILogger<AttachmentsController> logger) : ControllerBase
    {
        [HttpPost("designs/{designId}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachments(
            [FromRoute] int designId,
            CancellationToken ct)
        {
            logger.LogInformation("Upload request for design {DesignId}", designId);

            try
            {
                var httpRequest = HttpContext.Request;
                var files = httpRequest.Form.Files;

                logger.LogInformation("Received {Count} files", files?.Count ?? 0);

                if (files == null || files.Count == 0)
                {
                    logger.LogWarning("No files received in request");
                    return BadRequest(new { error = "At least one file is required" });
                }

                var fileList = files.ToList();
                var result = await attachmentService.CreateAsync(designId, fileList, ct);

                if (!result.IsSuccess)
                {
                    var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                    logger.LogWarning("Upload failed for design {DesignId}: {Error}",
                        designId, errorMessage);
                    return BadRequest(new { error = errorMessage });
                }

                logger.LogInformation("Upload successful for design {DesignId}", designId);

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
                logger.LogError(ex, "Error in UploadAttachments for design {DesignId}", designId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(
            [FromRoute] int id,
            CancellationToken ct)
        {
            logger.LogInformation("Delete request for attachment {AttachmentId}", id);

            var result = await attachmentService.RemoveAsync(id, ct);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                logger.LogWarning("Delete failed for attachment {AttachmentId}: {Error}",
                    id, errorMessage);
                return BadRequest(new { error = errorMessage });
            }

            logger.LogInformation("Delete successful for attachment {AttachmentId}", id);
            return Ok(new { message = "Attachment deleted successfully" });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadAttachment(
            [FromRoute] int id,
            CancellationToken ct)
        {
            logger.LogInformation("Download request for attachment {AttachmentId}", id);

            var attachmentResult = await attachmentService.GetAsync(id, ct);
            if (!attachmentResult.IsSuccess)
            {
                var errorMessage = attachmentResult.Errors?.FirstOrDefault() ?? "Unknown error";
                logger.LogWarning("Attachment {AttachmentId} not found: {Error}",
                    id, errorMessage);
                return NotFound(new { error = errorMessage });
            }

            var attachment = attachmentResult.Value;

            if (attachment == null || string.IsNullOrEmpty(attachment.RelativePath))
            {
                logger.LogWarning("Attachment {AttachmentId} has invalid data", id);
                return NotFound(new { error = "Attachment data is incomplete" });
            }

            var fileResult = await fileStorage.GetAsync(attachment.RelativePath);
            if (!fileResult.IsSuccess)
            {
                logger.LogError("File not found for attachment {AttachmentId}: {Path}",
                    id, attachment.RelativePath);
                return NotFound(new { error = "File not found on disk" });
            }

            var (stream, fileName, mimeType) = fileResult.Value;

            logger.LogInformation("Download successful for attachment {AttachmentId}", id);

            return File(stream, mimeType, fileName);
        }

        [HttpGet("designs/{designId}/attachments")]
        public async Task<IActionResult> GetDesignAttachments(
            [FromRoute] int designId,
            CancellationToken ct)
        {
            logger.LogInformation("Get attachments request for design {DesignId}", designId);

            var result = await attachmentService.GetDesignAttachmentsAsync(designId, ct);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                logger.LogWarning("Failed to get attachments for design {DesignId}: {Error}",
                    designId, errorMessage);
                return NotFound(new { error = errorMessage });
            }

            return Ok(new
            {
                designId,
                count = result.Value?.Count ?? 0,
                attachments = result.Value
            });
        }
        [HttpGet("designs/{designId}/attachments/download")]
        public async Task<IActionResult> DownloadAllAttachments(
            [FromRoute] int designId,
            CancellationToken ct)
        {
            logger.LogInformation("ZIP download request for design {DesignId}", designId);

            var result = await attachmentService.DownloadAllAttachmentsAsync(designId, ct);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Unknown error";
                logger.LogWarning("Failed to create ZIP for design {DesignId}: {Error}",
                    designId, errorMessage);
                return NotFound(new { error = errorMessage });
            }

            return File(result.Value.Content, "application/zip", result.Value.FileName);
        }
    }
}