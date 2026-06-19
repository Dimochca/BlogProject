using BlogProject.Models;
using BlogProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly ILogService _logService;

    public PostsController(IPostService postService, ICommentService commentService, ILogService logService)
    {
        _postService = postService;
        _commentService = commentService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? tagId)
    {
        var posts = await _postService.SearchAsync(search, tagId);
        _logService.LogAction("API: Получение списка статей", $"Search: {search}, TagId: {tagId}");
        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound(new { error = "Post not found" });

        _logService.LogAction("API: Получение статьи", $"PostId: {id}");
        return Ok(post);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _postService.CreateAsync(post, request.TagIds ?? new List<int>());
        _logService.LogAction("API: Создание статьи", $"Title: {request.Title}, UserId: {userId}");

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound(new { error = "Post not found" });

        if (post.AuthorId != userId.Value && !User.IsInRole("Admin"))
            return Forbid();

        post.Title = request.Title;
        post.Content = request.Content;
        post.UpdatedAt = DateTime.UtcNow;

        await _postService.UpdateAsync(post, request.TagIds ?? new List<int>());
        _logService.LogAction("API: Обновление статьи", $"PostId: {id}, UserId: {userId}");

        return Ok(post);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound(new { error = "Post not found" });

        if (post.AuthorId != userId.Value && !User.IsInRole("Admin"))
            return Forbid();

        await _postService.DeleteAsync(id);
        _logService.LogAction("API: Удаление статьи", $"PostId: {id}, UserId: {userId}");

        return NoContent();
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(int id)
    {
        var comments = await _commentService.GetByPostIdAsync(id);
        return Ok(comments);
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] int count = 3, [FromQuery] int days = 1)
    {
        var posts = await _postService.GetTopPostsByViewsAsync(count, days);
        return Ok(posts);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
    }
}

public class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<int>? TagIds { get; set; }
}

public class UpdatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<int>? TagIds { get; set; }
}