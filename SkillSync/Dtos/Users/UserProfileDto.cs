namespace SkillSync.Dtos.Users
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int DesignsCount { get; set; }
        public List<string> Roles { get; set; } = new();

    }
}
