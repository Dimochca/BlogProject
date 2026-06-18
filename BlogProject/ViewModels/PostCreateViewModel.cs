using System.ComponentModel.DataAnnotations;

namespace BlogProject.ViewModels
{
    public class PostCreateViewModel
    {
        [Required(ErrorMessage = "Заголовок обязателен")]
        [Display(Name = "Заголовок")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Заголовок должен быть от 3 до 200 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержание обязательно")]
        [Display(Name = "Содержание")]
        [MinLength(10, ErrorMessage = "Содержание должно быть не менее 10 символов")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Теги")]
        public List<int> SelectedTagIds { get; set; } = new List<int>();
    }
}