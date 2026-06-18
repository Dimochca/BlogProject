using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    [Authorize(Roles = "Owner,Admin")]
    public class AdminController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public AdminController(IUserService userService, IRoleService roleService, ILogService logService)
            : base(roleService, userService, logService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            await SetCurrentUserColorAsync();
            LogAction($"Открыта админ-панель", $"Пользователь: {GetCurrentUserId()}");
            return View();
        }

        public async Task<IActionResult> Users(string? searchTerm)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var users = await _userService.GetAllAsync();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    users = users.Where(u =>
                        u.UserName.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm)
                    ).ToList();
                }

                ViewBag.SearchTerm = searchTerm;
                LogAction($"Просмотр списка пользователей", $"Найдено: {users.Count()}, Поиск: {searchTerm ?? "без фильтра"}");
                return View(users);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке списка пользователей", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageUserRoles(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    LogAction($"Пользователь не найден для управления ролями", $"ID: {id}");
                    return NotFound();
                }

                var allRoles = await _roleService.GetAllAsync();
                var userRoles = await _roleService.GetUserRolesAsync(user.Id);

                ViewBag.AllRoles = allRoles;
                ViewBag.UserRoles = userRoles;
                ViewBag.User = user;

                LogAction($"Открыто управление ролями для пользователя", $"ID: {id}, Имя: {user.UserName}");
                return View();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке управления ролями для пользователя ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            try
            {
                if (roleId <= 0)
                {
                    TempData["Error"] = "Выберите роль для назначения.";
                    LogAction($"Попытка назначения роли без выбора", $"UserID: {userId}");
                    return RedirectToAction("ManageUserRoles", new { id = userId });
                }

                var currentUser = await _userService.GetByIdAsync(GetCurrentUserId());
                var targetUser = await _userService.GetByIdAsync(userId);
                var role = await _roleService.GetByIdAsync(roleId);

                if (targetUser == null || role == null)
                {
                    LogAction($"Ошибка при назначении роли", $"UserID: {userId}, RoleID: {roleId}");
                    return NotFound();
                }

                var currentUserRoles = await _roleService.GetUserRolesAsync(currentUser.Id);

                if (!currentUserRoles.Any(r => r.Name == "Owner"))
                {
                    var maxPosition = currentUserRoles.Any() ? currentUserRoles.Max(r => r.Position) : 0;
                    if (role.Position >= maxPosition)
                    {
                        TempData["Error"] = $"Вы не можете назначить роль '{role.Name}', так как её приоритет выше вашего.";
                        LogAction($"Попытка назначения роли без прав", $"UserID: {userId}, Role: {role.Name}");
                        return RedirectToAction("ManageUserRoles", new { id = userId });
                    }
                }

                var userRoles = await _roleService.GetUserRolesAsync(userId);
                if (userRoles.Any(r => r.Id == roleId))
                {
                    TempData["Error"] = $"У пользователя уже есть роль '{role.Name}'.";
                    return RedirectToAction("ManageUserRoles", new { id = userId });
                }

                await _roleService.AssignRoleToUserAsync(userId, roleId);
                LogAction($"Роль назначена", $"User: {targetUser.UserName}, Role: {role.Name}");
                TempData["Message"] = $"Роль {role.Name} назначена пользователю {targetUser.UserName}";
                return RedirectToAction("ManageUserRoles", new { id = userId });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при назначении роли пользователю ID: {userId}", ex);
                TempData["Error"] = "Произошла ошибка при назначении роли.";
                return RedirectToAction("ManageUserRoles", new { id = userId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(int userId, int roleId)
        {
            try
            {
                var currentUser = await _userService.GetByIdAsync(GetCurrentUserId());
                var targetUser = await _userService.GetByIdAsync(userId);
                var role = await _roleService.GetByIdAsync(roleId);

                if (targetUser == null || role == null)
                    return NotFound();

                var userRoles = await _roleService.GetUserRolesAsync(userId);
                if (userRoles.Count() <= 1)
                {
                    TempData["Error"] = "У пользователя должна быть хотя бы одна роль.";
                    return RedirectToAction("ManageUserRoles", new { id = userId });
                }

                var currentUserRoles = await _roleService.GetUserRolesAsync(currentUser.Id);

                if (!currentUserRoles.Any(r => r.Name == "Owner"))
                {
                    var maxPosition = currentUserRoles.Any() ? currentUserRoles.Max(r => r.Position) : 0;
                    if (role.Position >= maxPosition)
                    {
                        TempData["Error"] = $"Вы не можете снять роль '{role.Name}', так как её приоритет выше вашего.";
                        LogAction($"Попытка снятия роли без прав", $"UserID: {userId}, Role: {role.Name}");
                        return RedirectToAction("ManageUserRoles", new { id = userId });
                    }
                }

                await _roleService.RemoveRoleFromUserAsync(userId, roleId);
                LogAction($"Роль снята", $"User: {targetUser.UserName}, Role: {role.Name}");
                TempData["Message"] = $"Роль {role.Name} снята с пользователя {targetUser.UserName}";
                return RedirectToAction("ManageUserRoles", new { id = userId });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при снятии роли с пользователя ID: {userId}", ex);
                TempData["Error"] = "Произошла ошибка при снятии роли.";
                return RedirectToAction("ManageUserRoles", new { id = userId });
            }
        }

        public async Task<IActionResult> SwitchToUserMode()
        {
            LogAction($"Переключение в режим пользователя", $"Пользователь: {GetCurrentUserId()}");
            HttpContext.Session.SetString("UserMode", "true");
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> SwitchToAdminMode()
        {
            LogAction($"Возврат в режим администратора", $"Пользователь: {GetCurrentUserId()}");
            HttpContext.Session.Remove("UserMode");
            return RedirectToAction("Index", "Home");
        }
    }
}