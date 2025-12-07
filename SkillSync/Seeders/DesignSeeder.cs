using Microsoft.EntityFrameworkCore;
using SkillSync.Data.Entities;
using SkillSync.Data; 

namespace SkillSync.Seeders
{
    public class DesignSeeder : ISeeder
    {
        public SeederEnvironment Environment => SeederEnvironment.Development;

        private readonly AppDbContext _context;

        public DesignSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync(int rowsCount, SeederEnvironment currentEnvironment)
        {
            if (currentEnvironment != Environment && Environment != SeederEnvironment.Both)
            {
                return;
            }

            if (await _context.Designs.AnyAsync())
            {
                return;
            }

            var designs = DesignFaker.Generate(rowsCount);

            await _context.Designs.AddRangeAsync(designs);
            await _context.SaveChangesAsync();
        }
    }
}