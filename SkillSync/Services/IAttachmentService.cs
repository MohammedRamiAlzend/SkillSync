using SkillSync.Core;
using SkillSync.Data.Entities;

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
    }
}
