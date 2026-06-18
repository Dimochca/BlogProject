using System.ComponentModel.DataAnnotations.Schema;

namespace BlogProject.Models
{
    public class UserRole
    {
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}