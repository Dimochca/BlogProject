using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;
using BlogProject.ViewModels;

namespace BlogProject.Controllers
{
    [Authorize]
    public class PostsController : BaseController
    {
        private readonly IPostService _postService;
        private readonly ITagService _tagService;
        private readonly ICommentService _commentService;

        public PostsController(
            IPostService postService,
            ITagService tagService,
            ICommentService commentService,
            IUserService userService,
            IRoleService roleService,
            ILogService logService) : base(roleService, userService, logService)
        {
            _postService = postService;
            _tagService = tagService;
            _commentService = commentService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchText, int? tagId)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var posts = await _postService.SearchAsync(searchText, tagId);
                ViewBag.Tags = await _tagService.GetAllAsync();
                ViewBag.SearchText = searchText;
                ViewBag.SelectedTagId = tagId;

                var permissions = new Dictionary<int, (bool CanEdit, bool CanDelete)>();
                var colors = new Dictionary<int, string>();

                foreach (var post in posts)
                {
                    permissions[post.Id] = (await CanEditPost(post), await CanDeletePost(post));
                    colors[post.Id] = await GetUserColorAsync(post.Author);
                }

                ViewBag.Permissions = permissions;
                ViewBag.Colors = colors;

                LogAction($"Просмотр списка статей", $"Кол-во: {posts.Count()}, Поиск: {searchText ?? "без фильтра"}, Тег: {tagId?.ToString() ?? "все"}");
                return View(posts);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке списка статей", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var post = await _postService.GetByIdAsync(id);
                if (post == null)
                {
                    LogAction($"Статья не найдена", $"ID: {id}");
                    return NotFound();
                }

                await _postService.RecordViewAsync(id, Request.HttpContext.Connection.RemoteIpAddress?.ToString());

                var comments = await _commentService.GetByPostIdAsync(id);
                ViewBag.Comments = comments;

                ViewBag.CanEdit = await CanEditPost(post);
                ViewBag.CanDelete = await CanDeletePost(post);
                ViewBag.AuthorColor = await GetUserColorAsync(post.Author);

                var commentColors = new Dictionary<int, string>();
                foreach (var comment in comments)
                {
                    commentColors[comment.Id] = await GetUserColorAsync(comment.Author);
                }
                ViewBag.CommentColors = commentColors;

                LogAction($"Просмотр статьи", $"ID: {id}, Заголовок: {post.Title}");
                return View(post);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при просмотре статьи ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await SetCurrentUserColorAsync();

            try
            {
                ViewBag.Tags = await _tagService.GetAllAsync();
                LogAction($"Открыта форма создания статьи", $"Пользователь: {GetCurrentUserId()}");
                return View();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы создания статьи", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                LogAction($"Попытка создания статьи", $"Заголовок: {model.Title}");

                try
                {
                    var post = new Post
                    {
                        Title = model.Title,
                        Content = model.Content,
                        AuthorId = GetCurrentUserId(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _postService.CreateAsync(post, model.SelectedTagIds);
                    LogAction($"Статья создана", $"ID: {post.Id}, Заголовок: {post.Title}, Автор: {GetCurrentUserId()}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка при создании статьи: {model.Title}", ex);
                    ModelState.AddModelError("", "Произошла ошибка при создании статьи. Попробуйте ещё раз.");
                }
            }
            else
            {
                LogAction($"Ошибка валидации при создании статьи", $"Заголовок: {model.Title}");
            }

            try
            {
                ViewBag.Tags = await _tagService.GetAllAsync();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке тегов для формы создания", ex);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var post = await _postService.GetByIdAsync(id);
                if (post == null)
                {
                    LogAction($"Статья не найдена для редактирования", $"ID: {id}");
                    return NotFound();
                }

                if (!await CanEditPost(post))
                {
                    LogAction($"Попытка редактирования без прав", $"ID: {id}, Пользователь: {GetCurrentUserId()}");
                    return Forbid();
                }

                var model = new PostEditViewModel
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList()
                };

                ViewBag.Tags = await _tagService.GetAllAsync();
                LogAction($"Открыта форма редактирования статьи", $"ID: {id}, Заголовок: {post.Title}");
                return View(model);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы редактирования статьи ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostEditViewModel model)
        {
            await SetCurrentUserColorAsync();

            if (ModelState.IsValid)
            {
                LogAction($"Попытка редактирования статьи", $"ID: {model.Id}, Заголовок: {model.Title}");

                try
                {
                    var post = await _postService.GetByIdAsync(model.Id);
                    if (post == null)
                    {
                        LogAction($"Статья не найдена для редактирования", $"ID: {model.Id}");
                        return NotFound();
                    }

                    if (!await CanEditPost(post))
                    {
                        LogAction($"Попытка редактирования без прав", $"ID: {model.Id}, Пользователь: {GetCurrentUserId()}");
                        return Forbid();
                    }

                    post.Title = model.Title;
                    post.Content = model.Content;
                    post.UpdatedAt = DateTime.UtcNow;

                    await _postService.UpdateAsync(post, model.SelectedTagIds);
                    LogAction($"Статья обновлена", $"ID: {post.Id}, Заголовок: {post.Title}");
                    return RedirectToAction(nameof(Details), new { id = post.Id });
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка при редактировании статьи ID: {model.Id}", ex);
                    ModelState.AddModelError("", "Произошла ошибка при редактировании статьи. Попробуйте ещё раз.");
                }
            }
            else
            {
                LogAction($"Ошибка валидации при редактировании статьи", $"ID: {model.Id}, Заголовок: {model.Title}");
            }

            try
            {
                ViewBag.Tags = await _tagService.GetAllAsync();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке тегов для формы редактирования", ex);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var post = await _postService.GetByIdAsync(id);
                if (post == null)
                {
                    LogAction($"Статья не найдена для удаления", $"ID: {id}");
                    return NotFound();
                }

                if (!await CanDeletePost(post))
                {
                    LogAction($"Попытка удаления без прав", $"ID: {id}, Пользователь: {GetCurrentUserId()}");
                    return Forbid();
                }

                LogAction($"Открыта форма удаления статьи", $"ID: {id}, Заголовок: {post.Title}");
                return View(post);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы удаления статьи ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var post = await _postService.GetByIdAsync(id);
                if (post == null)
                {
                    LogAction($"Статья не найдена для удаления", $"ID: {id}");
                    return NotFound();
                }

                if (!await CanDeletePost(post))
                {
                    LogAction($"Попытка удаления без прав", $"ID: {id}, Пользователь: {GetCurrentUserId()}");
                    return Forbid();
                }

                var title = post.Title;
                await _postService.DeleteAsync(id);
                LogAction($"Статья удалена", $"ID: {id}, Заголовок: {title}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при удалении статьи ID: {id}", ex);
                TempData["Error"] = "Произошла ошибка при удалении статьи. Попробуйте ещё раз.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========== Методы проверки прав ==========
        private async Task<bool> CanEditPost(Post post)
        {
            var userId = GetCurrentUserId();

            if (await HasPermission("EditPosts"))
                return true;

            return post.AuthorId == userId;
        }

        private async Task<bool> CanDeletePost(Post post)
        {
            var userId = GetCurrentUserId();

            if (await HasPermission("DeletePosts"))
                return true;

            return post.AuthorId == userId;
        }
    }
}