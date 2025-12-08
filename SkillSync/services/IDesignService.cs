using SkillSync.Data.Entities;
using SkillSync.DTOs.Design;

namespace SkillSync.Services
{
    public interface IDesignService
    {
        // 🚨 التعديل هنا: الدالة تستقبل DTO مع IFormFile
        Task<Design> CreateDesignAsync(CreateDesignDto designDto);

        // ... الدوال الأخرى تبقى كما هي ...
        Task<IEnumerable<Design>> GetAllDesignsAsync();
        Task<Design?> GetDesignByIdAsync(int id);
        Task<bool> UpdateDesignAsync(int id, Design updatedDesign);
        Task<bool> DeleteDesignAsync(int id);
    }
}