namespace SkillSync.Dtos.Users
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DesignsCount { get; set; }

    }
}
