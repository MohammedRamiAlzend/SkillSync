using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Data.Repositories;
using SkillSync.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSync.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IGenericRepository<User> _userRepository;

        public AuthService(IConfiguration configuration, IGenericRepository<User> userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public async Task<AuthorizeResponse?> Login(string userName, string password)
        {
            var user = await _userRepository.GetAll()
       .Include(u => u.Roles)
       .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
                return null;

            if (user.PasswordHash != password)
                return null;

            var roles = user.Roles.Select(r => r.Name).ToList();

            if (!roles.Any())
            {
                throw new InvalidOperationException($"User '{user.UserName}' has no roles assigned");
            }

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
            }
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SkillSync";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "SkillSyncUsers";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
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