using BlogProject.Data;
using BlogProject.Models;
using BlogProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BlogProject.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUserNameAsync(string userName)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> RegisterAsync(string userName, string email, string password)
        {
            if (await UserExistsAsync(email))
                return false;

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return isValid ? user : null;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> CanUpdateProfileAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            if (user.LastProfileUpdate.HasValue && user.ProfileUpdateCount >= 3)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - user.LastProfileUpdate.Value;
                if (timeSinceLastUpdate.TotalMinutes < 5)
                    return false;
            }
            return true;
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.UserRoles?.Select(ur => ur.Role).ToList() ?? new List<Role>();
        }

        public async Task<string> GetUserMaxRoleColorAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return await GetUserMaxRoleColorAsync(user);
        }

        public async Task<string> GetUserMaxRoleColorAsync(User user)
        {
            if (user == null || user.UserRoles == null || !user.UserRoles.Any())
                return "#6c757d";

            var maxRole = user.UserRoles
                .OrderByDescending(ur => ur.Role.Position)
                .FirstOrDefault();

            return maxRole?.Role.Color ?? "#6c757d";
        }
    }
}