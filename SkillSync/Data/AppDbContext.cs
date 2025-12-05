using Microsoft.EntityFrameworkCore;
using SkillSync.Data.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SkillSync.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Design> Designs => Set<Design>();
        public DbSet<Attachment> Attachments => Set<Attachment>();


    }
}
