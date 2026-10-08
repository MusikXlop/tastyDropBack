namespace tastyDrop.Api.DTOs;

public class UpgradeRequestDto
{
    //предмет из инвентаря юзера
    public int InventoryId { get; set; }
    
    //предмет который он хочет
    public int TargetItemId { get; set; }
}
