namespace tastyDrop.Api.DTOs;

public class UpgradeResultDto
{
    //выиграл ли апгрейд
    public bool IsSuccess { get; set; }

    //рандом число от 0 до 100
    public double RollValue { get; set; }


    public double WinChance { get; set; }  // Рассчитанный шанс

    // Новый предмет в инвентаре (при успехе) и null если успеха нет
    public WonItemDto? WonItem { get; set; }
}
