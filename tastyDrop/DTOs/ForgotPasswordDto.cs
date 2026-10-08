namespace tastyDrop.Api.DTOs;

public class ForgotPasswordDto
{
    //почта который пользователь вводит при забытие пароля
    public string Email { get; set; } = string.Empty;
}
