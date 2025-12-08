using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSync.Core;
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

        public async Task<Result<AuthorizeResponse>> Login(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Result<AuthorizeResponse>.Failure("Username is required");

            if (string.IsNullOrWhiteSpace(password))
                return Result<AuthorizeResponse>.Failure("Password is required");

            var user = await _userRepository.GetAll()
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
                return Result<AuthorizeResponse>.Failure("Invalid username or password");

            if (user.PasswordHash != password)
                return Result<AuthorizeResponse>.Failure("Invalid username or password");

            var roles = user.Roles.Select(r => r.Name).ToList();
            if (!roles.Any())
                return Result<AuthorizeResponse>.Failure($"User '{user.UserName}' has no roles assigned");

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                return Result<AuthorizeResponse>.Failure("JWT configuration error: Key is missing");

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

            try
            {
                var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                var response = new AuthorizeResponse
                {
                    Token = tokenString,
                    UserId = user.Id,
                    UserName = user.UserName,
                    Roles = roles
                };

                return Result<AuthorizeResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<AuthorizeResponse>.Failure($"Token generation failed: {ex.Message}");
            }
        }
    }
}