using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSync.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;

        public AuthService(IConfiguration configuration, AppDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<AuthorizeResponse?> Login(string userName, string password)
        {
            // ابحثي عن User مع Roles باستخدام DbContext مباشرة
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
                return null;

            // تحقق من الباسوورد
            if (user.PasswordHash != password)
                return null;

            // احصلي على الـ Roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            var jwtKey = _configuration["Jwt:Key"] ?? "mySuperSecretKey123456789";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SkillSync";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "SkillSyncUsers";

            // Claims متعددة للـ Roles
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // أضيفي كل الـ Roles
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // إذا ما في roles خلي role واحدة
            if (!roles.Any())
            {
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthorizeResponse { Token = tokenString };
        }
    }
}