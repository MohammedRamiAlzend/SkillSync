using SkillSync.Core;

namespace SkillSync.Services
{
    public interface IFileStorageService
    {
            Task<Result<string>> SaveAsync(IFormFile file, CancellationToken ct);

            Task<Result> DeleteAsync(string relativePath);

            Task<Result<(Stream Stream, string FileName, string MimeType)>> GetAsync(string relativePath);
    }
}
