using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Services.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogService _logService;

    public UsersController(IUserService userService, ILogService logService)
    {
        _userService = userService;
        _logService = logService;
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(u => new { u.Id, u.UserName, u.Email, u.CreatedAt }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new { user.Id, user.UserName, user.Email, user.CreatedAt });
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetByUsername(string username)
    {
        var user = await _userService.GetByUserNameAsync(username);
        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new { user.Id, user.UserName, user.Email, user.CreatedAt });
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetUserRoles(int id)
    {
        var roles = await _userService.GetUserRolesAsync(id);
        return Ok(roles);
    }
}