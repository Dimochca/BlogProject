using BlogProject.Models;
using System.Security;

namespace BlogProject.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role> GetByIdAsync(int id);
        Task<Role> GetByNameAsync(string name);
        Task CreateAsync(Role role, List<int> permissionIds);
        Task UpdateAsync(Role role, List<int> permissionIds);
        Task DeleteAsync(int id);
        Task AssignRoleToUserAsync(int userId, int roleId);
        Task RemoveRoleFromUserAsync(int userId, int roleId);
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
    }
}