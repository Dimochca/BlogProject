using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogProject.Models;
using BlogProject.Services.Interfaces;

namespace BlogProject.Controllers
{
    [Authorize]
    public class CommentsController : BaseController
    {
        private readonly ICommentService _commentService;
        private readonly IPostService _postService;

        public CommentsController(ICommentService commentService, IPostService postService, ILogService logService)
            : base(null, null, logService)
        {
            _commentService = commentService;
            _postService = postService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int postId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["Error"] = "Комментарий не может быть пустым.";
                    LogAction($"Попытка создания пустого комментария", $"PostID: {postId}");
                    return RedirectToAction("Details", "Posts", new { id = postId });
                }

                var post = await _postService.GetByIdAsync(postId);
                if (post == null)
                {
                    LogAction($"Комментарий к несуществующему посту", $"PostID: {postId}");
                    return NotFound();
                }

                var comment = new Comment
                {
                    Content = content,
                    PostId = postId,
                    AuthorId = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow
                };

                await _commentService.CreateAsync(comment);
                LogAction($"Комментарий создан", $"PostID: {postId}, UserID: {GetCurrentUserId()}");
                return RedirectToAction("Details", "Posts", new { id = postId });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при создании комментария к посту ID: {postId}", ex);
                TempData["Error"] = "Произошла ошибка при создании комментария.";
                return RedirectToAction("Details", "Posts", new { id = postId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            await SetCurrentUserColorAsync();

            try
            {
                var comment = await _commentService.GetByIdAsync(id);
                if (comment == null)
                {
                    LogAction($"Комментарий не найден для удаления", $"ID: {id}");
                    return NotFound();
                }

                if (!await CanDeleteComment(comment))
                {
                    LogAction($"Попытка удаления комментария без прав", $"ID: {id}, UserID: {GetCurrentUserId()}");
                    return Forbid();
                }

                LogAction($"Открыта форма удаления комментария", $"ID: {id}");
                return View(comment);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при открытии формы удаления комментария ID: {id}", ex);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var comment = await _commentService.GetByIdAsync(id);
                if (comment == null)
                {
                    LogAction($"Комментарий не найден для удаления", $"ID: {id}");
                    return NotFound();
                }

                if (!await CanDeleteComment(comment))
                {
                    LogAction($"Попытка удаления комментария без прав", $"ID: {id}, UserID: {GetCurrentUserId()}");
                    return Forbid();
                }

                var postId = comment.PostId;
                await _commentService.DeleteAsync(id);
                LogAction($"Комментарий удалён", $"ID: {id}");
                return RedirectToAction("Details", "Posts", new { id = postId });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при удалении комментария ID: {id}", ex);
                TempData["Error"] = "Произошла ошибка при удалении комментария.";
                return RedirectToAction("Details", "Posts", new { id = id });
            }
        }

        private async Task<bool> CanDeleteComment(Comment comment)
        {
            var userId = GetCurrentUserId();

            if (await HasPermission("DeleteComments"))
                return true;

            return comment.AuthorId == userId;
        }
    }
}