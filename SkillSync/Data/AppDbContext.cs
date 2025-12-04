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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole composite key
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Design — User (N:1)
            modelBuilder.Entity<Design>()
                .HasOne(d => d.User)
                .WithMany(u => u.Designs)
                .HasForeignKey(d => d.UserId);

            // Attachment — Design (N:1)
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Design)
                .WithMany(d => d.Attachments)
                .HasForeignKey(a => a.DesignId);

            // Attachment — User (N:1)
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.OwnerUser)
                .WithMany(u => u.Attachments)
                .HasForeignKey(a => a.OwnerUserId);
        }

    }
}
