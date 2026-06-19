using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogService _logService;

    public TagsController(ITagService tagService, ILogService logService)
    {
        _tagService = tagService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await _tagService.GetAllAsync();
        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tag = await _tagService.GetByIdAsync(id);
        if (tag == null)
            return NotFound(new { error = "Tag not found" });

        return Ok(tag);
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request)
    {
        var tag = new Tag { Name = request.Name };
        await _tagService.CreateAsync(tag);
        _logService.LogAction("API: Создание тега", $"Name: {request.Name}");

        return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag);
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTagRequest request)
    {
        var tag = await _tagService.GetByIdAsync(id);
        if (tag == null)
            return NotFound(new { error = "Tag not found" });

        tag.Name = request.Name;
        await _tagService.UpdateAsync(tag);
        _logService.LogAction("API: Обновление тега", $"TagId: {id}, Name: {request.Name}");

        return Ok(tag);
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _tagService.DeleteAsync(id);
        _logService.LogAction("API: Удаление тега", $"TagId: {id}");
        return NoContent();
    }
}

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTagRequest
{
    public string Name { get; set; } = string.Empty;
}