using BlogProject.Models;
using BlogProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IPostService _postService;
    private readonly ILogService _logService;

    public CommentsController(ICommentService commentService, IPostService postService, ILogService logService)
    {
        _commentService = commentService;
        _postService = postService;
        _logService = logService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return NotFound(new { error = "Comment not found" });

        return Ok(comment);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var post = await _postService.GetByIdAsync(request.PostId);
        if (post == null)
            return NotFound(new { error = "Post not found" });

        var comment = new Comment
        {
            Content = request.Content,
            PostId = request.PostId,
            AuthorId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _commentService.CreateAsync(comment);
        _logService.LogAction("API: Создание комментария", $"PostId: {request.PostId}, UserId: {userId}");

        return CreatedAtAction(nameof(GetById), new { id = comment.Id }, comment);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return NotFound(new { error = "Comment not found" });

        if (comment.AuthorId != userId.Value && !User.IsInRole("Admin"))
            return Forbid();

        await _commentService.DeleteAsync(id);
        _logService.LogAction("API: Удаление комментария", $"CommentId: {id}, UserId: {userId}");

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
    }
}

public class CreateCommentRequest
{
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
}