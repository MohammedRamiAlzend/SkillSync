using SkillSync.Dtos.Users;

namespace SkillSync.services
{
    public interface IUserService
    {
        Task<IEnumerable<UserListDto>> GetAllUsersAsync(string? search);
        Task<UserPagedResultDto> GetUsersPagedAsync(int pageNumber, int pageSize, string? search);
        Task<UserProfileDto?> GetUserProfileAsync(int id);
    }
}
