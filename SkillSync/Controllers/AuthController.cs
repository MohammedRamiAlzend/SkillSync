using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSync.Services;
using SkillSync.Models;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Login(request.UserName, request.Password);
            if (result == null)
                return Unauthorized(new { message = "Invalid credentials" });

            return Ok(result);
        }

        [HttpGet("protected")]
        [Authorize]
        public IActionResult Protected()
        {
            var user = User.Identity?.Name ?? "unknown";
            return Ok(new { message = $"Hello {user}, you are authenticated." });
        }
    }
}




