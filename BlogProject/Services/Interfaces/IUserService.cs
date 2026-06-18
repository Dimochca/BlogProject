using BlogProject.Models;

namespace BlogProject.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetByIdAsync(int id);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUserNameAsync(string userName);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> RegisterAsync(string userName, string email, string password);
        Task<User?> AuthenticateAsync(string email, string password);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> UserExistsAsync(string email);
        Task<bool> CanUpdateProfileAsync(int userId);
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);
        Task<string> GetUserMaxRoleColorAsync(int userId);
        Task<string> GetUserMaxRoleColorAsync(User user);
    }
}