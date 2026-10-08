using Microsoft.IdentityModel.Logging;

namespace tastyDrop.Api.DTOs;

public class CrashStartDto
{
    public decimal BetAmount { get; set; }
}

public class CrashCashoutDto
{
    public int GameId { get; set; } //id game 
    public decimal TargetMultiplier { get; set; }// множитель
}

public class CrashGameEntity
{
    public int GameId { get; set; }
    public decimal BetAmount { get; set; }
    
    public decimal CrashMultiplier { get; set; }

    public decimal? CashoutMultiplier { get; set; }

    public decimal WinAmount { get; set; } 

    public string Status { get; set; } = string.Empty; //статус гры
    public DateTime CreatedAt { get; set; } 
}

public class CrashGameStatusDto
{
    public int GameId { get; set; }
    public decimal BetAmount { get; set; }

    //nullво время игры 
    //заполняется ток после краша или вывода 
    public decimal? CrashPoint { get; set; } // точка краша
    public decimal CurrentMultiplier { get; set; } //текущий множитель
    public string Status { get; set; } = "InGame"; // InGame Won Crashed статусы игры 
    public decimal NewBalance { get; set; }
    public decimal WinAmount { get; set; } = 0; // сумма выигрыша
}
