namespace SkillSync.Dtos
{
    public class AttachmentDto
    {

            public int Id { get; set; }
            public string FileName { get; set; } = null!;
            public string MimeType { get; set; } = null!;
            public long FileSizeBytes { get; set; }
            public bool IsPrimary { get; set; }
            public DateTime CreatedAt { get; set; }
        }
}
