using Microsoft.IdentityModel.Tokens;
using SkillSync.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSync.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private const string StaticUserName = "test";
        private const string StaticPassword = "123";

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<AuthorizeResponse?> Login(string userName, string password)
        {
            if (userName == StaticUserName && password == StaticPassword)
            {
                var jwtKey = _configuration["Jwt:Key"] ?? "mySuperSecretKey123456789";
                var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SkillSync";
                var jwtAudience = _configuration["Jwt:Audience"] ?? "SkillSyncUsers";

                if (!int.TryParse(_configuration["Jwt:DurationMinutes"], out int durationMinutes))
                {
                    durationMinutes = 60;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("UserId", "1"), 
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(durationMinutes);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                var response = new AuthorizeResponse
                {
                    Token = tokenString,
                    ExpiresAt = expires
                };

                return Task.FromResult<AuthorizeResponse?>(response);
            }

            return Task.FromResult<AuthorizeResponse?>(null);
        }
    }
}