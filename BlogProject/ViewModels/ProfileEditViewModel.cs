using System.ComponentModel.DataAnnotations;

namespace BlogProject.ViewModels
{
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [Display(Name = "Имя пользователя")]
        [StringLength(15, MinimumLength = 3, ErrorMessage = "Имя должно быть от 3 до 15 символов")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я0-9_-]+$",
            ErrorMessage = "Имя пользователя может содержать только буквы, цифры, дефис и подчёркивание")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Введите корректный email (например, user@domain.com)")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Новый пароль")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        public string? NewPassword { get; set; }

        [Display(Name = "Подтверждение нового пароля")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string? ConfirmNewPassword { get; set; }
    }
}