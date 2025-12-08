using Microsoft.AspNetCore.Mvc;
using SkillSync.Data.Entities;
using SkillSync.DTOs.Design;
using SkillSync.Services;

[ApiController]
[Route("api/[controller]")]
public class DesignController : ControllerBase
{
    private readonly IDesignService _designService;

    public DesignController(IDesignService designService)
    {
        _designService = designService;
    }

    // [HTTP POST] لإنشاء تصميم جديد مع رفع ملف
    [HttpPost]
    // Consumes تحدد أن Endpoint تتوقع بيانات من نوع 'multipart/form-data'
    // يتم استخدام [FromForm] مع الـ DTO لاستقبال البيانات النصية والملف
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Design>> CreateDesign([FromForm] CreateDesignDto designDto)
    {
        // 1. التحقق من صحة النموذج (يشمل التحقق من وجود الملف المطلوب)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // 400 Bad Request
        }

        // 2. تحقق إضافي لحجم ونوع الملف (اختياري لكن موصى به)
        const int MAX_FILE_SIZE_MB = 10;
        if (designDto.File.Length == 0)
        {
            return BadRequest("The uploaded file is empty.");
        }
        if (designDto.File.Length > MAX_FILE_SIZE_MB * 1024 * 1024)
        {
            return BadRequest($"File size exceeds {MAX_FILE_SIZE_MB}MB limit.");
        }

        // 3. استدعاء الخدمة لحفظ الملف وحفظ الكيان في DB
        var createdDesign = await _designService.CreateDesignAsync(designDto);

        // 4. إرجاع 201 Created
        return CreatedAtAction(nameof(GetDesignById), new { id = createdDesign.Id }, createdDesign);
    }

    // [HTTP GET] جلب جميع التصاميم: GET /api/design
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Design>>> GetAllDesigns()
    {
        var designs = await _designService.GetAllDesignsAsync();
        return Ok(designs); // 200 OK
    }

    // [HTTP GET] جلب تصميم محدد: GET /api/design/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Design>> GetDesignById(int id)
    {
        var design = await _designService.GetDesignByIdAsync(id);

        if (design == null)
        {
            return NotFound(); // 404 Not Found
        }

        return Ok(design);
    }

    // [HTTP PUT] تحديث تصميم: PUT /api/design/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDesign(int id, [FromBody] Design design)
    {
        // 💡 ملاحظة: عملية التحديث هذه لا تشمل تحديث الملف. إذا أردت تحديث الملف، يجب عليك إنشاء DTO آخر
        // ونقطة نهاية (Endpoint) منفصلة أو دمجها بعناية.

        if (id != design.Id)
        {
            return BadRequest("Design ID mismatch."); // 400
        }

        var isSuccess = await _designService.UpdateDesignAsync(id, design);

        if (!isSuccess)
        {
            return NotFound(); // 404
        }

        return NoContent(); // 204 No Content
    }

    // [HTTP DELETE] حذف تصميم: DELETE /api/design/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDesign(int id)
    {
        var isSuccess = await _designService.DeleteDesignAsync(id);

        if (!isSuccess)
        {
            return NotFound(); // 404
        }

        return NoContent(); // 204 No Content
    }
}