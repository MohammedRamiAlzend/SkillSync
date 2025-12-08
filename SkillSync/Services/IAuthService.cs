using SkillSync.Core;
using SkillSync.Models;

namespace SkillSync.Services
{
    public interface IAuthService
    {
        Task<Result<AuthorizeResponse>> Login(string userName, string password);
    }
}