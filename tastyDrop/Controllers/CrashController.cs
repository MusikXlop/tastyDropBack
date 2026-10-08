using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Claims;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrashController : ControllerBase
{
    private readonly IDbService _dbService;    //сервис для работы с БД



   

    public CrashController(IDbService dbService)
    {
        _dbService = dbService;
    }

    private int GetUserIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value; //доставать айди юзера из токена
        return int.Parse(claim!); //переобзарует в инт


    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveGame()
    {
        int userId = GetUserIdFromToken(); //для получания токена айди игрока
        var result = await _dbService.GetActiveCrashGameAsync(userId); //поиск активной игры 
        return Ok(result); //возвращаем результатыч
    }

    [HttpPost("cashout")]                      // POST api/crash/cashout
    public async Task<IActionResult> Cashout([FromBody] CrashCashoutDto dto)
    {
        try
        {
            int userId = GetUserIdFromToken(); // получаем ID игрока
            var result = await _dbService.CrashCashoutAsync(userId, dto.GameId, dto.TargetMultiplier); //баланс и умножалка
            return Ok(result);                 // возвращаем результат
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message }); // 
        }
    }
}
