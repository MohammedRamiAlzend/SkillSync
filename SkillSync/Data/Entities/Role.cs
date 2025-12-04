namespace SkillSync.Data.Entities
{
    public class Role
    {
        public int Id { get; set; }          // PK Id
        public string Name { get; set; } = null!; // Role name: Admin, Participant, ...

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
   
    }
}

