using Microsoft.AspNetCore.Http; // يجب إضافة هذا المرجع
using System.ComponentModel.DataAnnotations;

namespace SkillSync.DTOs.Design
{
    public class CreateDesignDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int UserId { get; set; }

        // ✨ الإضافة الرئيسية: استقبال الملف المرفوع ✨
        [Required(ErrorMessage = "Design file is required.")]
        public IFormFile File { get; set; } = null!; // لتمثيل الملف الذي تم رفعه
    }
}