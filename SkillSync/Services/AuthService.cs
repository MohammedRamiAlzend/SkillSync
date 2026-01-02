using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSync.Core;
using SkillSync.Data.Entities;
using SkillSync.Data.Repositories;
using SkillSync.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace SkillSync.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Role> _roleRepository;

        public AuthService(IConfiguration configuration, IGenericRepository<User> userRepository, IGenericRepository<Role> roleRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
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

            //if (user.PasswordHash != password)
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
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




        public async Task<Result<AuthorizeResponse>> Register(string userName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Result<AuthorizeResponse>.Failure("A username is required.");

            if (string.IsNullOrWhiteSpace(email))
                return Result<AuthorizeResponse>.Failure("A email is required.");

            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
            }
            catch
            {
                return Result<AuthorizeResponse>.Failure("The email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(password))
                return Result<AuthorizeResponse>.Failure("A password is required.");

            if (password.Length < 8)
                return Result<AuthorizeResponse>.Failure("The password must be at least 8 characters long.");


            var existingUserByName = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (existingUserByName != null)
                return Result<AuthorizeResponse>.Failure("The username already exists.");

            var existingUserByEmail = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUserByEmail != null)
                return Result<AuthorizeResponse>.Failure("The email address is already in use.");



            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var newUser = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = passwordHash,
            };

            var userRole = await _roleRepository.GetAll()
    .FirstOrDefaultAsync(r => r.Name == "User");

            if (userRole == null)
            {
                userRole = new Role { Name = "User" };
                await _roleRepository.AddAsync(userRole);
                await _roleRepository.SaveChangesAsync();
            }

            newUser.Roles.Add(userRole);


            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
    //        var testRoles = await _userRepository.GetAll()
    //.Include(u => u.Roles)
    //.Where(u => u.Id == newUser.Id)
    //.Select(u => u.Roles.Select(r => r.Name).ToList())
    //.FirstOrDefaultAsync();

    //        Console.WriteLine($"Roles after save: {string.Join(", ", testRoles)}");

            var roles =  newUser.Roles.Select(r => r.Name).ToList();           
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                return Result<AuthorizeResponse>.Failure("JWT configuration error: Key is missing");

            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SkillSync";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "SkillSyncUsers";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, newUser.UserName),
                new Claim("UserId", newUser.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.Add(new Claim(ClaimTypes.Role, "User"));

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
                    UserId = newUser.Id,
                    UserName = newUser.UserName,
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