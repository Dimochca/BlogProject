using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;
using BlogProject.ViewModels;
using BCrypt.Net;

namespace BlogProject.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;

        public ProfileController(IUserService userService, IPostService postService, ILogService logService)
            : base(null, userService, logService)
        {
            _userService = userService;
            _postService = postService;
        }

        public async Task<IActionResult> Index()
        {
            await SetCurrentUserColorAsync();

            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound();

                var posts = await _postService.GetByAuthorIdAsync(userId);
                ViewBag.Posts = posts;
                LogAction($"Просмотр профиля", $"UserID: {userId}");
                return View(user);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке профиля", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            await SetCurrentUserColorAsync();

            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound();

                if (user.LastProfileUpdate.HasValue && user.ProfileUpdateCount >= 3)
                {
                    var timeSinceLastUpdate = DateTime.UtcNow - user.LastProfileUpdate.Value;
                    if (timeSinceLastUpdate.TotalMinutes < 5)
                    {
                        var remainingSeconds = (int)(300 - timeSinceLastUpdate.TotalSeconds);
                        TempData["Error"] = $"Вы исчерпали лимит изменений (3 за 5 минут). Подождите {remainingSeconds} секунд.";
                        LogAction($"Попытка редактирования профиля сверх лимита", $"UserID: {userId}");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        user.ProfileUpdateCount = 0;
                        await _userService.UpdateAsync(user);
                    }
                }

                var model = new ProfileEditViewModel
                {
                    UserName = user.UserName,
                    Email = user.Email
                };

                LogAction($"Открыта форма редактирования профиля", $"UserID: {userId}");
                return View(model);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы редактирования профиля", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            await SetCurrentUserColorAsync();

            if (!ModelState.IsValid)
            {
                LogAction($"Ошибка валидации при редактировании профиля", $"UserID: {GetCurrentUserId()}");
                return View(model);
            }

            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound();

                if (user.LastProfileUpdate.HasValue && user.ProfileUpdateCount >= 3)
                {
                    var timeSinceLastUpdate = DateTime.UtcNow - user.LastProfileUpdate.Value;
                    if (timeSinceLastUpdate.TotalMinutes < 5)
                    {
                        TempData["Error"] = "Лимит изменений превышен. Попробуйте через 5 минут.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        user.ProfileUpdateCount = 0;
                    }
                }

                var existingUserByName = await _userService.GetByUserNameAsync(model.UserName);
                if (existingUserByName != null && existingUserByName.Id != userId)
                {
                    ModelState.AddModelError("UserName", "Пользователь с таким именем уже существует");
                    LogAction($"Ошибка редактирования профиля", $"Имя {model.UserName} уже занято");
                    return View(model);
                }

                var existingUserByEmail = await _userService.GetByEmailAsync(model.Email);
                if (existingUserByEmail != null && existingUserByEmail.Id != userId)
                {
                    ModelState.AddModelError("Email", "Пользователь с таким email уже существует");
                    LogAction($"Ошибка редактирования профиля", $"Email {model.Email} уже занят");
                    return View(model);
                }

                user.UserName = model.UserName;
                user.Email = model.Email;

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    if (model.NewPassword.Length < 6)
                    {
                        ModelState.AddModelError("NewPassword", "Пароль должен быть не менее 6 символов");
                        return View(model);
                    }
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    LogAction($"Смена пароля", $"UserID: {userId}");
                }

                if (user.LastProfileUpdate.HasValue &&
                    (DateTime.UtcNow - user.LastProfileUpdate.Value).TotalMinutes < 5)
                {
                    user.ProfileUpdateCount += 1;
                }
                else
                {
                    user.ProfileUpdateCount = 1;
                }
                user.LastProfileUpdate = DateTime.UtcNow;

                await _userService.UpdateAsync(user);
                LogAction($"Профиль обновлён", $"UserID: {userId}");

                TempData["Message"] = "Профиль успешно обновлён!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при редактировании профиля", ex);
                TempData["Error"] = "Произошла ошибка при редактировании профиля.";
                return View(model);
            }
        }
    }
}