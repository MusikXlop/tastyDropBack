using Microsoft.AspNetCore.Mvc;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Controllers;

[ApiController] //автоматическая валидацичя
[Route("api/[controller]")] //api/items маршрут
public class ItemsController : ControllerBase
{
    private readonly IDbService _dbService;

    public ItemsController(IDbService dbService)
    {
        _dbService = dbService;
    }
    [HttpGet("all")]
    public async Task<IActionResult> GetAllItems()
    {
        var items = await _dbService.GetAllItemsAsync();
        return Ok(items);
    }
}

    

