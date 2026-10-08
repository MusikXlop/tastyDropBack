namespace tastyDrop.Api.DTOs;

//внутреняя модель юзера *для ворка с бд
public class UserDto
{
    public int UserId { get; set; } //id юзера из таблицы Users
    public string UserName { get; set; } = string.Empty; //имя
    public string UserEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public decimal Balance { get; set; }


    public int BattlePassBalance { get; set; }

    public string Role { get; set; } = "User";

    public bool IsEmailConfirmed { get; set; }
}
