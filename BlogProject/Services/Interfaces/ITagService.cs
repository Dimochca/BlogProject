using BlogProject.Models;

namespace BlogProject.Services.Interfaces
{
    public interface ITagService
    {
        Task<Tag> GetByIdAsync(int id);
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<Tag> GetByNameAsync(string name);
        Task CreateAsync(Tag tag);
        Task UpdateAsync(Tag tag);
        Task DeleteAsync(int id);
        Task<bool> TagExistsAsync(string name);
    }
}