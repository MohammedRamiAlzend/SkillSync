using Microsoft.EntityFrameworkCore;
using SkillSync.Data;
using SkillSync.Dtos.Users;
using SkillSync.services;

namespace SkillSync.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserListDto>> GetAllUsersAsync(string? search)
        {
            IQueryable<Data.Entities.User> query = _context.Users
                .Include(u => u.Designs);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(search));
            }

            var users = await query.ToListAsync();

            var result = users.Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                DesignsCount = u.Designs.Count
            });

            return result;
        }

        public async Task<UserPagedResultDto> GetUsersPagedAsync(int pageNumber, int pageSize, string? search)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            IQueryable<Data.Entities.User> query = _context.Users
                .Include(u => u.Designs);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = users.Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                DesignsCount = u.Designs.Count
            }).ToList();

            return new UserPagedResultDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Designs)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            var profile = new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                DesignsCount = user.Designs.Count,
                Roles = user.Roles.Select(r => r.Name).ToList(),
            };

            return profile;
        }
    }
}