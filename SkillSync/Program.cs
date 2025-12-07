using Microsoft.EntityFrameworkCore;
using SkillSync.Data.Repositories;
using Scalar.AspNetCore;
using SkillSync.Data;
using SkillSync.Services;
using SkillSync.services;
using SkillSync.Seeders; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDesignService, DesignService>();


builder.Services.AddScoped<ISeeder, DesignSeeder>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        await SeedDataAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


async Task SeedDataAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;

    var currentEnv = services.GetRequiredService<IWebHostEnvironment>();
    SeederEnvironment appEnvironment = currentEnv.IsDevelopment() ? SeederEnvironment.Development : SeederEnvironment.Production;

    var seeders = services.GetServices<ISeeder>();

    const int DEFAULT_ROWS_COUNT = 50;

    foreach (var seeder in seeders)
    {
        Console.WriteLine($"Starting seeding for: {seeder.GetType().Name}...");
        await seeder.SeedAsync(DEFAULT_ROWS_COUNT, appEnvironment);
        Console.WriteLine($"Finished seeding for: {seeder.GetType().Name}.");
    }
}