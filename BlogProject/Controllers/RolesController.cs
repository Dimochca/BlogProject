using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    [Authorize(Roles = "Owner")]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;

        public RolesController(IRoleService roleService, IUserService userService, ILogService logService)
            : base(roleService, userService, logService)
        {
            _roleService = roleService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            await SetCurrentUserColorAsync();

            try
            {
                var roles = await _roleService.GetAllAsync();
                LogAction($"Просмотр списка ролей", $"Кол-во: {roles.Count()}");
                return View(roles);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке списка ролей", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await SetCurrentUserColorAsync();

            try
            {
                ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
                LogAction($"Открыта форма создания роли");
                return View();
            }
            catch (Exception ex)
            {
                LogError("Ошибка при открытии формы создания роли", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role, List<int> selectedPermissions)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userService.GetByIdAsync(GetCurrentUserId());
                    var currentUserRoles = await _roleService.GetUserRolesAsync(currentUser.Id);

                    if (!currentUserRoles.Any(r => r.Name == "Owner"))
                    {
                        var maxPosition = currentUserRoles.Any() ? currentUserRoles.Max(r => r.Position) : 0;
                        if (role.Position >= maxPosition)
                        {
                            ModelState.AddModelError("Position", $"Позиция роли ({role.Position}) не может быть выше или равна вашей максимальной позиции ({maxPosition}).");
                            LogAction($"Попытка создания роли с позицией выше своей", $"Position: {role.Position}, Max: {maxPosition}");
                            ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
                            return View(role);
                        }
                    }

                    await _roleService.CreateAsync(role, selectedPermissions ?? new List<int>());
                    LogAction($"Роль создана", $"Name: {role.Name}, Position: {role.Position}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка при создании роли {role.Name}", ex);
                    ModelState.AddModelError("", "Произошла ошибка при создании роли.");
                }
            }

            ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
            return View(role);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var role = await _roleService.GetByIdAsync(id);
                if (role == null)
                {
                    LogAction($"Роль не найдена для редактирования", $"ID: {id}");
                    return NotFound();
                }

                if (role.IsSystem)
                {
                    TempData["Error"] = "Системные роли нельзя редактировать";
                    LogAction($"Попытка редактирования системной роли", $"ID: {id}");
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
                ViewBag.SelectedPermissions = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
                LogAction($"Открыта форма редактирования роли", $"ID: {id}, Name: {role.Name}");
                return View(role);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы редактирования роли ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role, List<int> selectedPermissions)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRole = await _roleService.GetByIdAsync(role.Id);
                    if (existingRole == null || existingRole.IsSystem)
                    {
                        TempData["Error"] = "Системные роли нельзя редактировать";
                        LogAction($"Попытка редактирования системной роли", $"ID: {role.Id}");
                        return RedirectToAction(nameof(Index));
                    }

                    var currentUser = await _userService.GetByIdAsync(GetCurrentUserId());
                    var currentUserRoles = await _roleService.GetUserRolesAsync(currentUser.Id);

                    if (!currentUserRoles.Any(r => r.Name == "Owner"))
                    {
                        var maxPosition = currentUserRoles.Any() ? currentUserRoles.Max(r => r.Position) : 0;
                        if (role.Position >= maxPosition)
                        {
                            ModelState.AddModelError("Position", $"Позиция роли ({role.Position}) не может быть выше или равна вашей максимальной позиции ({maxPosition}).");
                            LogAction($"Попытка изменения позиции роли выше своей", $"Position: {role.Position}, Max: {maxPosition}");
                            ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
                            ViewBag.SelectedPermissions = selectedPermissions ?? new List<int>();
                            return View(role);
                        }
                    }

                    await _roleService.UpdateAsync(existingRole, selectedPermissions ?? new List<int>());
                    LogAction($"Роль обновлена", $"ID: {role.Id}, Name: {role.Name}, Position: {role.Position}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка при редактировании роли ID: {role.Id}", ex);
                    ModelState.AddModelError("", "Произошла ошибка при редактировании роли.");
                }
            }

            ViewBag.Permissions = await _roleService.GetAllPermissionsAsync();
            return View(role);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var role = await _roleService.GetByIdAsync(id);
                if (role == null)
                {
                    LogAction($"Роль не найдена для удаления", $"ID: {id}");
                    return NotFound();
                }

                if (role.IsSystem)
                {
                    TempData["Error"] = "Системные роли нельзя удалять";
                    LogAction($"Попытка удаления системной роли", $"ID: {id}");
                    return RedirectToAction(nameof(Index));
                }

                LogAction($"Открыта форма удаления роли", $"ID: {id}, Name: {role.Name}");
                return View(role);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы удаления роли ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var role = await _roleService.GetByIdAsync(id);
                if (role != null && !role.IsSystem)
                {
                    var name = role.Name;
                    await _roleService.DeleteAsync(id);
                    LogAction($"Роль удалена", $"ID: {id}, Name: {name}");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при удалении роли ID: {id}", ex);
                TempData["Error"] = "Произошла ошибка при удалении роли.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}