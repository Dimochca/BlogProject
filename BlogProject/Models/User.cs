using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [RegularExpression(@"^[a-zA-Zа-яА-Я0-9_-]+$",
            ErrorMessage = "Имя пользователя может содержать только буквы, цифры, дефис и подчёркивание")]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastProfileUpdate { get; set; }
        public int ProfileUpdateCount { get; set; } = 0;
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}