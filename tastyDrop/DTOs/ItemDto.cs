namespace tastyDrop.Api.DTOs;

public class ItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public string Rarity { get; set; } = "";
    public int DropWeight { get; set; } //  шанс выпадения
}
