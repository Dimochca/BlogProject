using System.ComponentModel.DataAnnotations.Schema;
using System.Security;

namespace BlogProject.Models
{
    public class RolePermission
    {
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        [ForeignKey("Permission")]
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}