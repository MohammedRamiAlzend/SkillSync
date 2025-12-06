using Microsoft.AspNetCore.Mvc;
using SkillSync.Dtos.Users;
using SkillSync.services;
using SkillSync.Services;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? search)
        {
            var users = await _userService.GetAllUsersAsync(search);
            return Ok(users);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetUsersPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _userService.GetUsersPagedAsync(pageNumber, pageSize, search);

            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();

            return Ok(result.Items);
        }

        [HttpGet("{id:int}/profile")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var profile = await _userService.GetUserProfileAsync(id);

            if (profile == null)
                return NotFound("User not found");

            return Ok(profile);
        }
    }
}