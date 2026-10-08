namespace tastyDrop.Api.DTOs
{
    //отправка ангуляру информацию о выпавшем предмете 
   
    public class WonItemDto
    {
        public int DropId { get; set; }
        public int InventoryId { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        public decimal Price { get; set; }
        public string Rarity { get; set; } = "";

        public bool IsFreeOpen { get; set; }
    }
}
