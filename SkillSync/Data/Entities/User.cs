namespace SkillSync.Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<Design> Designs { get; set; } = new List<Design>();

    }
}

