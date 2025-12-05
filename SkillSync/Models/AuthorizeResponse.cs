namespace SkillSync.Models
{
    public class AuthorizeResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
