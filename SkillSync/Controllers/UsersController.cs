using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Data.Repositories;
using SkillSync.Dtos.Users;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {


          private readonly IGenericRepository<User> _userRepo;
    private readonly IGenericRepository<Attachment> _attachmentRepo;
    private readonly AppDbContext _context;

    public UsersController(
        IGenericRepository<User> userRepo,
        IGenericRepository<Attachment> attachmentRepo,
        AppDbContext context)
    {
        _userRepo = userRepo;
        _attachmentRepo = attachmentRepo;
        _context = context;
    }

    // =========================================
    // 1) GET ALL USERS + FILTERING BY NAME
    // GET: api/users?search=ahmad
    // =========================================
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? search)
    {
        IQueryable<User> query = _context.Users
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

        return Ok(result);
    }

    // =========================================
    // 2) GET ALL USERS PAGINATED
    // GET: api/users/paged?pageNumber=1&pageSize=10&search=ahmad
    // =========================================
    [HttpGet("paged")]
    public async Task<IActionResult> GetUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<User> query = _context.Users
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

        var result = users.Select(u => new UserListDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            DesignsCount = u.Designs.Count
        });

        Response.Headers["X-Total-Count"] = totalCount.ToString();

        return Ok(result);
    }

    // =========================================
    // 3) GET USER PROFILE
    // GET: api/users/{id}/profile
    // =========================================
    [HttpGet("{id:int}/profile")]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        var user = await _context.Users
            .Include(u => u.Designs)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound("User not found");
            // Retrieve the user's primary image if available
            var avatar = await _context.Attachments
            .FirstOrDefaultAsync(a =>
                a.OwnerUserId == user.Id &&
                a.IsPrimary &&
                a.IsActive);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            DesignsCount = user.Designs.Count,
            Roles = user.UserRoles.Select(r => r.Role.Name).ToList(),
            AvatarPath = avatar?.RelativePath
        };

        return Ok(profile);
    }

}
    
}
