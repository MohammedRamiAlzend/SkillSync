using SkillSync.Data.Entities;
using SkillSync.DTOs;
using SkillSync.DTOs.Design;

namespace SkillSync.Services
{
    public class DesignService : IDesignService
    {
        private readonly List<Design> _designs = new List<Design>
        {
            new Design { Id = 1, Title = "SkillSync Logo", UserId = 1, Status = "Approved" }
        };
        private int _nextId = 2; 

        public async Task<Design> CreateDesignAsync(CreateDesignDto designDto)
        {
            var newDesign = new Design
            {
                Id = _nextId++,
                UserId = designDto.UserId,
                Title = designDto.Title,
                Description = designDto.Description,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending" 
            };

            _designs.Add(newDesign);
            return newDesign;
        }

        public async Task<IEnumerable<Design>> GetAllDesignsAsync()
        {
            return _designs.Where(d => !d.IsDeleted);
        }

        public async Task<Design?> GetDesignByIdAsync(int id)
        {
            return _designs.FirstOrDefault(d => d.Id == id && !d.IsDeleted);
        }

        public async Task<bool> UpdateDesignAsync(int id, Design updatedDesign)
        {
            var existingDesign = _designs.FirstOrDefault(d => d.Id == id && !d.IsDeleted);
            if (existingDesign == null)
            {
                return false;
            }

            existingDesign.Title = updatedDesign.Title;
            existingDesign.Description = updatedDesign.Description;
            existingDesign.Status = updatedDesign.Status;
            existingDesign.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public async Task<bool> DeleteDesignAsync(int id)
        {
            var designToDelete = _designs.FirstOrDefault(d => d.Id == id && !d.IsDeleted);
            if (designToDelete == null)
            {
                return false;
            }

            designToDelete.IsDeleted = true;
            designToDelete.UpdatedAt = DateTime.UtcNow;

            return true;
        }
    }
}