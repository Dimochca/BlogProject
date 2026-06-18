using System.ComponentModel.DataAnnotations;

namespace BlogProject.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введите email или имя пользователя")]
        [Display(Name = "Email или имя пользователя")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; } = false;
    }
}