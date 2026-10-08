namespace tastyDrop.Api.DTOs;

public class LiveDropDto
{
    public int DropId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemImage { get; set; } = string.Empty;
    public string ItemRarity { get; set; } = string.Empty;
    public int? CaseId { get; set; }
    public string CaseName { get; set; } = string.Empty;
    public string CaseImage { get; set; } = string.Empty;
    public DateTime DropTime { get; set; }
}
