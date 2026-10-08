namespace tastyDrop.Api.DTOs;

//класс для приема данных при входе на сайт
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
