using Microsoft.AspNetCore.Mvc;
using SkillSync.Data;

namespace SkillSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TestController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var usersCount = _db.Users.Count();
            return Ok(new { message = "EF Core OK", usersCount });
        }
        }
}
