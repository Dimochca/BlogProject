using BlogProject.Models;
using BlogProject.Services.Interfaces;
using BlogProject.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogProject.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public AccountController(IUserService userService, IRoleService roleService, ILogService logService)
            : base(roleService, userService, logService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await SetCurrentUserColorAsync();
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                LogAction($"Попытка регистрации", $"Username: {model.UserName}, Email: {model.Email}");

                if (await _userService.UserExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Пользователь с таким email уже зарегистрирован.");
                    LogAction($"Регистрация отклонена", $"Email {model.Email} уже существует");
                    return View(model);
                }

                var success = await _userService.RegisterAsync(model.UserName, model.Email, model.Password);
                if (success)
                {
                    var user = await _userService.GetByEmailAsync(model.Email);

                    var userRole = await _roleService.GetByNameAsync("User");
                    if (userRole != null)
                    {
                        await _roleService.AssignRoleToUserAsync(user.Id, userRole.Id);
                    }

                    LogAction($"Регистрация успешна", $"Пользователь {model.UserName} создан");
                    await SignInAsync(user);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    LogAction($"Регистрация не удалась", $"Пользователь {model.UserName}");
                    ModelState.AddModelError("", "Ошибка регистрации. Попробуйте ещё раз.");
                }
            }
            else
            {
                LogAction($"Регистрация отклонена", $"Ошибка валидации для {model.UserName}");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            await SetCurrentUserColorAsync();

            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                User? user = null;
                if (model.Login.Contains('@'))
                    user = await _userService.GetByEmailAsync(model.Login);
                else
                    user = await _userService.GetByUserNameAsync(model.Login);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    LogAction($"Успешный вход", $"Пользователь: {user.UserName}");
                    await SignInAsync(user, model.RememberMe);
                    return RedirectToAction("Index", "Home");
                }

                LogAction($"Неудачная попытка входа", $"Login: {model.Login}");
                ModelState.AddModelError("", "Неверный логин или пароль.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                LogAction($"Выход из системы", $"Пользователь: {User.Identity.Name}");
            }
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInAsync(Models.User user, bool isPersistent = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var userRoles = await _userService.GetUserRolesAsync(user.Id);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
        }
    }
}