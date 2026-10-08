namespace tastyDrop.Api.DTOs;

public class WheelSpinDto
{
    //ставка
    public decimal BetAmount { get; set; }

    //цвет
    public string TargetColor { get; set; } = "";
  
}


public class WheelResultDto
{
    public int GameId { get; set; } // id игры
    public decimal BetAmount { get; set; } //ставка
    //выбранный цвет
    public string TargetColor { get; set; } = "";

    //выпавший цвет
    public string WonColor { get; set; } = "";

    //индекст сектора на фронте
    public int WinningSegmentIndex { get; set; }

    //множитель
    public decimal Multiplier { get; set; }

    //сумма вина
    public decimal WinAmount { get; set; }
    //нью баланс
    public decimal NewBalance { get; set; }

}

public class WheelSegmentEntity
{
    public string Color { get; set; } = ""; // цвет сектора
    public decimal Multiplier {  get; set; } //множитель
    public int Weight { get; set; } // шанс
}
