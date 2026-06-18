using BlogProject.Models;

namespace BlogProject.Services.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> GetByIdAsync(int id);
        Task<IEnumerable<Comment>> GetByPostIdAsync(int postId);
        Task CreateAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int id);
    }
}