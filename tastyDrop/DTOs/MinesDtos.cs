namespace tastyDrop.Api.DTOs;

public class MinesStartDto
{
    public decimal BetAmount { get; set; } //сумма ставки
    public int MinesCount { get; set; } //мины от 1 до 14

    //РАЗМЕР СЕТКИ С ФРОНТЕНТА 3 4 5 6 
    public int GridSize { get; set; } = 5;
}

public class MinesRevealDto
{
    public int GameId { get; set; } //id текущей игры
    public int CellIndex { get; set; } //индекст клетки от 0 до 24 которую юзер открывает


}
public class MinesCashoutDto
{
    public int GameId { get; set; }    // id текущей игры
}

public class MinesGameEntity
{
    public int GameId { get; set; }
    public decimal BetAmount { get; set; }
    public int MinesCount { get; set; }

    //Сохраняем размер сетки для текущей игры
    public int GridSize { get; set; } = 5;

    public decimal CurrentMultiplier { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class MinesGridCellEntity
{
    public bool IsMine { get; set; }
    public bool IsRevealed { get; set; }
}



public class MinesGameStatusDto
{
    public int GameId { get; set; } // ID игры
    public decimal BetAmount { get; set; } // ставка игрока
    public int MinesCount { get; set; } // количество мин в игре
    public decimal CurrentMultiplier { get; set; } // текущий множитель выигрыша
    public string Status { get; set; } = "InGame"; // статус игры (InGame Won Lost)
    public decimal NewBalance { get; set; } // актуальный баланс юзера
    public List<int> RevealedCells { get; set; } = new(); // список открытых клеток

    //Возвращаем размер сетки клиенту
    public int GridSize { get; set; } = 5;

    public bool IsHitMine { get; set; } = false; // попал ли игрок на мину
    public decimal WinAmount { get; set; } = 0; // сумма выигрыша 
    public List<int>? MinePositions { get; set; } // позиции мин только при выводи денег или вузе 
}