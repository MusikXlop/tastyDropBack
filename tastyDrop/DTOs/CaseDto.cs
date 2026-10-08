namespace tastyDrop.Api.DTOs;

//данные кейса для главной страницы
public class CaseDto
{
    public int CaseId { get; set; } 
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";
}
