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

    [HttpPost]
    public async Task<ActionResult<Design>> CreateDesign([FromBody] CreateDesignDto designDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); 
        }

        var createdDesign = await _designService.CreateDesignAsync(designDto);

        return CreatedAtAction(nameof(GetDesignById), new { id = createdDesign.Id }, createdDesign);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Design>>> GetAllDesigns()
    {
        var designs = await _designService.GetAllDesignsAsync();
        return Ok(designs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Design>> GetDesignById(int id)
    {
        var design = await _designService.GetDesignByIdAsync(id);

        if (design == null)
        {
            return NotFound(); 
        }

        return Ok(design);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDesign(int id, [FromBody] Design design)
    {
        if (id != design.Id)
        {
            return BadRequest("Design ID mismatch."); 
        }

        var isSuccess = await _designService.UpdateDesignAsync(id, design);

        if (!isSuccess)
        {
            return NotFound();
        }

        return NoContent(); 
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDesign(int id)
    {
        var isSuccess = await _designService.DeleteDesignAsync(id);

        if (!isSuccess)
        {
            return NotFound();
        }

        return NoContent(); 
    }
}