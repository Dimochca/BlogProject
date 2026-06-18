using System.ComponentModel.DataAnnotations;

namespace BlogProject.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public bool IsSystem { get; set; } = false;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}