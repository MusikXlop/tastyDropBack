namespace tastyDrop.Api.DTOs;
//dto для ответа после того как юзер забрал награду
public class ClaimRewardResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal MainBalance { get; set; }          // Основной баланс 
    public int BattlePassBalance { get; set; }       //Баттлпасса баланс 2
    public bool ClaimedCase { get; set; } // Был ли выдан кейс
}
