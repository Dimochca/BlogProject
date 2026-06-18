using BlogProject.Models;

namespace BlogProject.Services.Interfaces
{
    public interface IPostService
    {
        Task<Post> GetByIdAsync(int id);
        Task<IEnumerable<Post>> GetAllAsync();
        Task<IEnumerable<Post>> GetByAuthorIdAsync(int authorId);
        Task<IEnumerable<Post>> SearchAsync(string? searchText, int? tagId);
        Task CreateAsync(Post post, List<int> tagIds);
        Task UpdateAsync(Post post, List<int> tagIds);
        Task DeleteAsync(int id);
        Task RecordViewAsync(int postId, string? userIdentifier = null);
        Task<IEnumerable<Post>> GetTopPostsByViewsAsync(int count, int days = 1);
    }
}