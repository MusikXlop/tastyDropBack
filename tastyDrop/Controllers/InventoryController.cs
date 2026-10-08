
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tastyDrop.Api.Services;
using tastyDrop.Api.DTOs;

namespace tastyDrop.Api.Controllers;




[ApiController] //атрибут (api контроллер) 
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IDbService _dbService;

    public InventoryController(IDbService dbService)
    {
        _dbService = dbService; //внедрение зависимостей
    }

    [HttpPost("sell")]
    public async Task<IActionResult> SellItem([FromBody] SellItemDto dto)
    {
        //достаем UserId из токена 
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { Message = "Польхзователь не авторизован " });
        }

        try
        {
            // вызываем DbService
            var newBalance = await _dbService.SellItemAsync(userId, dto.InventoryId);
            return Ok(new { Success = true, NewBalance = newBalance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message});
        }
    }

    // POST: api/inventory/withdraw
    [HttpPost("withdraw")]
    public async Task<IActionResult> WithDrawItem([FromBody] WithdrawItemDto dto)
    {
        try
        {
            //извлекаем userId из jwt 
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _dbService.WithdrawInventoryItemAsync(dto.InventoryId, userId);

            return Ok(new { Message = "Предмет отправлен на ваш акк" });
        }
        catch (Exception ex)
        {
            return BadRequest(new {Message = ex.Message});
        }
    }

}


