using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogProject.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Author")]
        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;

        [ForeignKey("Post")]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
    }
}