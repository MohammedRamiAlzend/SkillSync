using System.ComponentModel.DataAnnotations;

namespace SkillSync.DTOs.Design
{
    public class CreateDesignDto
    {
        // نطلب فقط البيانات المدخلة من المستخدم
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // نحتاج لمعرف المستخدم الذي يقوم بالإنشاء
        public int UserId { get; set; }
    }
}