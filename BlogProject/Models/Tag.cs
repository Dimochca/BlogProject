using System.ComponentModel.DataAnnotations;

namespace BlogProject.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}