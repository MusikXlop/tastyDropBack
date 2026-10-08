using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Controllers;


[ApiController] //коонтроллер
[Route("api/[controller]")] //маршрут
[Authorize] //авторизация над
public class MinesController : ControllerBase
{
    private readonly IDbService _dbSerivce;

    public MinesController(IDbService dbSerivce)
    {
        //DI
        _dbSerivce = dbSerivce;
    }

    private int GetUserIdFromToken()
    {
        //достаем айди пользовтеля из jwt токна
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        //переобразуем строку в инт 
        return int.Parse(claim!);
    }

    [HttpGet("active")] // GET api/mines/active
    public async Task<IActionResult> GetActiveGame()
    {
        //полуаейт айди юзера из токена
        int userId = GetUserIdFromToken();
        //получаем активную игру из бд
        var result = await _dbSerivce.GetActiveMinesGameAsync(userId);
        //возврат результата в формату jjson 
        return Ok(result);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromBody] MinesStartDto dto)
    {
        try
        {
            //id игрока из токена
            int userId = GetUserIdFromToken();
            //запуск новый игры через сервис 
            var result = await _dbSerivce.MinesStartGameAsync(userId, dto.BetAmount, dto.MinesCount, dto.GridSize);
            //возвращаем dto с данными игры
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reveal")]
    public async Task<IActionResult> RevealCell([FromBody] MinesRevealDto dto)
    {
        try
        {
            int userId = GetUserIdFromToken(); //id playera

            //открываем клетку через сервис
            var result = await _dbSerivce.MinesRevealCellAsync(userId, dto.GameId, dto.CellIndex);
            //возвращаем обновленный стастус игры
            return Ok(result);

        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message }); 
        }
    }

    [HttpPost("cashout")]
    public async Task<IActionResult> Cashout([FromBody] MinesCashoutDto dto)
    {
        try
        {
            int userId = GetUserIdFromToken();
            var result = await _dbSerivce.MinesCashoutAsync(userId, dto.GameId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
