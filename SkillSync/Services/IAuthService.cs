using SkillSync.Models;

namespace SkillSync.Services
{
    public interface IAuthService
    {
        Task<AuthorizeResponse?> Login(string userName, string password);
    }
}