using System.ComponentModel.DataAnnotations;

namespace tastyDrop.Api.DTOs;
   public class ConfirmEmailDto
   {
    [Required] // поле обязательно
    [EmailAddress] //проверка шо строка
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)] //код ровно 6 символов
    public string Code { get; set; } = string.Empty;
   }

