namespace tastyDrop.Api.DTOs;

//dto для продажи предмета 
public class SellItemDto
{
    public int InventoryId { get; set; } // ID записи в инвентаре

}
//вывод предмета
public class WithdrawItemDto
{
    public int InventoryId { get; set; } // ID записи в инвентаре

}

public class InventoryDtos
{
    public int InventoryId { get; set; } // ID записи в UserInventory
    public int ItemId { get; set; }      // ID предмета
    public string Name { get; set; } = string.Empty; // Название предмета
    public string ImageUrl { get; set; } = string.Empty; // Картинка
    public decimal Price { get; set; }   // Цена предмета
    public string Rarity { get; set; } = string.Empty; // Редкость
    public DateTime? DroppedAt { get; set; } // Дата выпадения
}

public class BestDropDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Rarity { get; set; } = string.Empty;
    public DateTime DroppedAt { get; set; }
}