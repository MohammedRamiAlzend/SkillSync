namespace SkillSync.Data.Entities
{
    public class Design
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string Status { get; set; } = "Pending";
        public bool IsDeleted { get; set; } = false;

        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
