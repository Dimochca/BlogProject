using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IRoleService? _roleService;
        protected IUserService? _userService;
        protected ILogService _logService;

        protected BaseController(ILogService logService)
        {
            _logService = logService;
        }

        protected BaseController(IRoleService roleService, IUserService userService, ILogService logService)
        {
            _roleService = roleService;
            _userService = userService;
            _logService = logService;
        }

        protected bool IsAdminInCurrentMode()
        {
            if (!User.IsInRole("Admin")) return false;
            var userMode = HttpContext.Session.GetString("UserMode");
            return userMode != "true";
        }

        protected bool IsAdminOrModeratorInCurrentMode()
        {
            if (IsAdminInCurrentMode()) return true;
            if (User.IsInRole("Moderator")) return true;
            return false;
        }

        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        protected async Task<bool> HasPermission(string permissionName)
        {
            if (_roleService == null)
                return false;

            var userId = GetCurrentUserId();
            return await _roleService.HasPermissionAsync(userId, permissionName);
        }

        protected async Task<string> GetUserColorAsync(int userId)
        {
            if (_userService == null)
                return "#6c757d";

            try
            {
                return await _userService.GetUserMaxRoleColorAsync(userId);
            }
            catch
            {
                return "#6c757d";
            }
        }

        protected async Task<string> GetUserColorAsync(Models.User user)
        {
            if (_userService == null)
                return "#6c757d";

            try
            {
                return await _userService.GetUserMaxRoleColorAsync(user);
            }
            catch
            {
                return "#6c757d";
            }
        }

        protected async Task SetCurrentUserColorAsync()
        {
            try
            {
                if (User.Identity.IsAuthenticated && _userService != null)
                {
                    var userId = GetCurrentUserId();
                    var user = await _userService.GetByIdAsync(userId);
                    if (user != null)
                    {
                        ViewBag.UserColor = await _userService.GetUserMaxRoleColorAsync(user);
                        return;
                    }
                }

                ViewBag.UserColor = "#6c757d";
            }
            catch
            {
                ViewBag.UserColor = "#6c757d";
            }
        }

        protected void LogAction(string action, string details = "")
        {
            var userName = User.Identity?.Name ?? "Anonymous";
            _logService.LogAction($"{action} | User: {userName}", details);
        }

        protected void LogError(string message, Exception? ex = null)
        {
            _logService.LogError(message, ex);
        }
    }
}