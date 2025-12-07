using SkillSync.Data.Entities;
using SkillSync.DTOs.Design;

namespace SkillSync.Services
{
    public interface IDesignService
    {
        // نستخدم DTO عند الإنشاء
        Task<Design> CreateDesignAsync(CreateDesignDto designDto);

        Task<IEnumerable<Design>> GetAllDesignsAsync();
        Task<Design?> GetDesignByIdAsync(int id);

        // عند التحديث، نمرر الكيان المحدّث (يفضل استخدام DTO خاص بالتحديث أيضاً)
        Task<bool> UpdateDesignAsync(int id, Design updatedDesign);
        Task<bool> DeleteDesignAsync(int id);
    }
}