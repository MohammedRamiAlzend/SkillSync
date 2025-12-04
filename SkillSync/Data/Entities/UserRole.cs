namespace SkillSync.Data.Entities
{
    public class UserRole
    {
        public int UserId { get; set; }      // FK إلى Users.Id
        public User User { get; set; } = null!;

        public int RoleId { get; set; }      // FK إلى Roles.Id
        public Role Role { get; set; } = null!;
    }
}
