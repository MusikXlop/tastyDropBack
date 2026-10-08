using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WheelController : ControllerBase
{
    private readonly IDbService _dbService;

    public WheelController(IDbService dbService)
    {
        _dbService = dbService;
    }

    private int GetUserIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return int.Parse(claim!); // достаем userId из JWT токена
    }

    [HttpPost("spin")]
    public async Task<IActionResult> Spin([FromBody] WheelSpinDto dto)
    {
        try
        {
            int userId = GetUserIdFromToken(); // получаем ID игрока
            var result = await _dbService.WheelSpinAsync(userId, dto.BetAmount, dto.TargetColor);
            return Ok(result); // возвращаем результат
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message }); // обработка ошибок
        }
    }
}

