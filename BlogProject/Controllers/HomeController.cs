using Microsoft.AspNetCore.Mvc;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IPostService _postService;

        public HomeController(IPostService postService, IUserService userService, IRoleService roleService, ILogService logService)
            : base(roleService, userService, logService)
        {
            _postService = postService;
        }

        public async Task<IActionResult> Index()
        {
            await SetCurrentUserColorAsync();

            try
            {
                var topPosts = await _postService.GetTopPostsByViewsAsync(3);
                ViewBag.TopPosts = topPosts;
                LogAction("Просмотр главной страницы");
                return View();
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке главной страницы", ex);
                return View();
            }
        }

        public async Task<IActionResult> AccessDenied()
        {
            await SetCurrentUserColorAsync();
            LogAction("Просмотр страницы 'Доступ запрещён'");
            return View();
        }

        public async Task<IActionResult> NotFound()
        {
            await SetCurrentUserColorAsync();
            LogAction("Просмотр страницы 'Не найдено'");
            return View();
        }

        public async Task<IActionResult> Error()
        {
            await SetCurrentUserColorAsync();
            LogAction("Просмотр страницы ошибки");
            return View();
        }
    }
}