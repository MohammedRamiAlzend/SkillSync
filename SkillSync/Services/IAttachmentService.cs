using SkillSync.Core;
using SkillSync.Data.Entities;
using SkillSync.Dtos;

namespace SkillSync.Services
{
    public interface IAttachmentService
    {
        Task<Result<List<Attachment>>> CreateAsync(
            int designId,
            List<IFormFile> files,
            CancellationToken ct);

        Task<Result> RemoveAsync(
            int attachmentId,
            CancellationToken ct);

        Task<Result<Attachment>> GetAsync(
            int attachmentId,
            CancellationToken ct);

        Task<Result<List<AttachmentDto>>> GetDesignAttachmentsAsync(
                int designId,
                CancellationToken ct);

        Task<Result<ZipFileDto>> DownloadAllAttachmentsAsync(
            int designId,
            CancellationToken ct);
    }
    }
