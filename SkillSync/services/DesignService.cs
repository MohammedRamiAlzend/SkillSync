using SkillSync.Data.Entities;
using SkillSync.DTOs.Design;
using Microsoft.AspNetCore.Hosting; // نحتاجها لمعرفة مسار الويب
using Microsoft.EntityFrameworkCore; // إذا كنت تستخدم Entity Framework Core

namespace SkillSync.Services
{
    public class DesignService : IDesignService
    {
        // محاكاة لـ DbContext أو مجموعة بيانات
        private readonly List<Design> _designs = new List<Design>
        {
            // بيانات افتراضية
            new Design { Id = 1, Title = "SkillSync Logo", UserId = 1, Status = "Approved" }
        };
        private int _nextId = 2;

        // ✨ حقن بيئة الاستضافة لمعرفة مسارات المجلدات ✨
        private readonly IWebHostEnvironment _hostingEnvironment;
        // private readonly ApplicationDbContext _context; // إذا كنت تستخدم EF Core

        // يجب إضافة IWebHostEnvironment إلى المُنشئ (Constructor)
        public DesignService(IWebHostEnvironment hostingEnvironment
            /*, ApplicationDbContext context */)
        {
            _hostingEnvironment = hostingEnvironment;
            // _context = context; 
        }

        // 🚨 الدالة الخاصة بإنشاء التصميم (مع معالجة الملف) 🚨
        public async Task<Design> CreateDesignAsync(CreateDesignDto designDto)
        {
            // 1. تحديد مسار الحفظ (مثال: داخل مجلد "designs" في wwwroot)
            var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "designs");

            // التأكد من وجود المجلد
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 2. توليد اسم ملف فريد لحماية نظام الملفات
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + designDto.File.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 3. حفظ الملف في نظام الملفات بشكل غير متزامن
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await designDto.File.CopyToAsync(fileStream);
            }

            // 4. إنشاء كيان Design وحفظ المسار النسبي
            var newDesign = new Design
            {
                Id = _nextId++,
                UserId = designDto.UserId,
                Title = designDto.Title,
                Description = designDto.Description,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                // 💡 ملاحظة: يجب أن يحتوي كيان Design على خاصية FileUrl لتمثيل مسار الملف! 
                // سنستخدم Description كبديل مؤقت في هذا المثال لعدم وجود الخاصية FileUrl في الكيان الذي زودتني به.
                // FileUrl = "/designs/" + uniqueFileName 
            };

            // في مثالنا الذي لا يحتوي على FileUrl، سنستخدم الوصف لحفظ المسار مؤقتاً:
            newDesign.Description = $"File saved at: /designs/{uniqueFileName}";

            _designs.Add(newDesign);
            // في EF Core: await _context.Designs.AddAsync(newDesign); await _context.SaveChangesAsync();
            return newDesign;
        }

        // ----------------------------------------------------
        // الدوال المتبقية (READ, UPDATE, DELETE) تبقى كما هي تقريباً
        // ----------------------------------------------------

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
            if (existingDesign == null) return false;

            existingDesign.Title = updatedDesign.Title;
            existingDesign.Description = updatedDesign.Description;
            existingDesign.Status = updatedDesign.Status;
            existingDesign.UpdatedAt = DateTime.UtcNow;

            // ... (منطق تحديث الـ DbContext)
            return true;
        }

        public async Task<bool> DeleteDesignAsync(int id)
        {
            var designToDelete = _designs.FirstOrDefault(d => d.Id == id && !d.IsDeleted);
            if (designToDelete == null) return false;

            designToDelete.IsDeleted = true;
            designToDelete.UpdatedAt = DateTime.UtcNow;

            // ... (منطق حذف الـ DbContext)
            return true;
        }
    }
}