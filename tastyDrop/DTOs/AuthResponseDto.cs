namespace tastyDrop.Api.DTOs;

//класс ответа котолрый отправляется клиенту  после успеха(логина и регистрации)
public class AuthResponseDto
{
    // сгенерированный jwt токен
    public string Token { get; set; } = string.Empty;

    //имя типа для показа в интерфейсе 
    public string Username { get; set; } = string.Empty;

    //текущий бананс юзера
    public decimal Balance { get; set; }

    public int UserId { get; set; }

    public int BattlePassBalance { get; set; }
}
