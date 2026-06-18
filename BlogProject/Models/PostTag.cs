using System.ComponentModel.DataAnnotations.Schema;

namespace BlogProject.Models
{
    public class PostTag
    {
        [ForeignKey("Post")]
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        [ForeignKey("Tag")]
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}