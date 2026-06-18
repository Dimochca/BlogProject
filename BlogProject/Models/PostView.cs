using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogProject.Models
{
    public class PostView
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Post")]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public string? UserIdentifier { get; set; }
    }
}