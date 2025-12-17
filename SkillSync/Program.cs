using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SkillSync.Data;
using SkillSync.Data.Entities;
using SkillSync.Data.Repositories;
using SkillSync.services;
using SkillSync.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Add services to the container
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>(); 
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IGenericRepository<User>, GenericRepository<User>>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Authentication
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

EnsureStorageDirectories(builder);

var app = builder.Build();

app.UseStaticFiles();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Ensure database is created
    dbContext.Database.EnsureCreated();

    // Add roles if they don't exist
    if (!dbContext.Roles.Any())
    {
        dbContext.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "User" },
            new Role { Name = "Designer" }
        );
        dbContext.SaveChanges();
    }

    if (!dbContext.Users.Any())
    {
        var testUser = new User
        {
            UserName = "test",
            Email = "test@example.com",
            PasswordHash = "123",
            IsActive = true
        };

        dbContext.Users.Add(testUser);
        dbContext.SaveChanges();

        var userRole = dbContext.Roles.FirstOrDefault(r => r.Name == "User");
        if (userRole != null)
        {
            testUser.Roles.Add(userRole);
            dbContext.SaveChanges();
        }

        Console.WriteLine("Database seeded with test user: test / 123");
    }

    if (!dbContext.Designs.Any())
    {
        var design = new Design
        {
            UserId = 1,
            Title = "Test Design",
            Description = "For testing attachments",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Designs.Add(design);
        dbContext.SaveChanges();
        Console.WriteLine("Created test design with ID: 1");
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

static void EnsureStorageDirectories(WebApplicationBuilder builder)
{
    try
    {
        var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            Directory.CreateDirectory(wwwrootPath);
            Console.WriteLine($"Created wwwroot directory at: {wwwrootPath}");
        }

        var uploadsPath = Path.Combine(wwwrootPath, "Uploads");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
            Console.WriteLine($"Created Uploads directory at: {uploadsPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating storage directories: {ex.Message}");
    }
}