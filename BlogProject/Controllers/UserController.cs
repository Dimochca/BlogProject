using Microsoft.AspNetCore.Mvc;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;

        public UserController(IUserService userService, IPostService postService, ILogService logService)
            : base(null, userService, logService)
        {
            _userService = userService;
            _postService = postService;
        }

        public async Task<IActionResult> Details(string username)
        {
            await SetCurrentUserColorAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    LogAction($"Попытка просмотра профиля без имени");
                    return NotFound();
                }

                var user = await _userService.GetByUserNameAsync(username);
                if (user == null)
                {
                    LogAction($"Пользователь не найден", $"Username: {username}");
                    return NotFound();
                }

                var posts = await _postService.GetByAuthorIdAsync(user.Id);
                ViewBag.Posts = posts;

                LogAction($"Просмотр профиля пользователя", $"Username: {username}, ID: {user.Id}");
                return View(user);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при просмотре профиля пользователя {username}", ex);
                return RedirectToAction("Error", "Home");
            }
        }
    }
}