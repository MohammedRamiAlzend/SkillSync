namespace SkillSync.Dtos.Users
{
    public class UserPagedResultDto
    {
        public List<UserListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
