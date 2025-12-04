namespace SkillSync.Data.Entities
{
    public class Attachment
    {
        public int Id { get; set; }

        public int? DesignId { get; set; }
        public Design? Design { get; set; }

        public int OwnerUserId { get; set; }
        public User OwnerUser { get; set; } = null!;

        public string FileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public long FileSizeBytes { get; set; }

        public string? RelativePath { get; set; }
        public string StorageType { get; set; } = "FileSystem";

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
