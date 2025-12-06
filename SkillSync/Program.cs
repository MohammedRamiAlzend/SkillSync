using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkillSync.Services; 

using Microsoft.EntityFrameworkCore;
using SkillSync.Data.Repositories;
using Scalar.AspNetCore;
using SkillSync.Data;


var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SkillSync",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SkillSyncUsers",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "mySuperSecretKey123456789")),
        ClockSkew = TimeSpan.Zero 
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Ensure database is created
    dbContext.Database.EnsureCreated();

    // Add roles if they don't exist
    if (!dbContext.Roles.Any())
    {
        dbContext.Roles.AddRange(
            new SkillSync.Data.Entities.Role { Name = "Admin" },
            new SkillSync.Data.Entities.Role { Name = "User" },
            new SkillSync.Data.Entities.Role { Name = "Designer" }
        );
        dbContext.SaveChanges();
    }

    if (!dbContext.Users.Any())
    {
        var testUser = new SkillSync.Data.Entities.User
        {
            UserName = "test",
            Email = "test@example.com",
            PasswordHash = "123", 
            IsActive = true
        };

        dbContext.Users.Add(testUser);
        dbContext.SaveChanges();

        var userRole = new SkillSync.Data.Entities.UserRole
        {
            UserId = testUser.Id,
            RoleId = dbContext.Roles.First(r => r.Name == "User").Id
        };

        dbContext.UserRoles.Add(userRole);
        dbContext.SaveChanges();

        Console.WriteLine("Database seeded with test user: test / 123");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();