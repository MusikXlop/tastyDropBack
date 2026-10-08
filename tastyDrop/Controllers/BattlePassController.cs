using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class BattlePassController : ControllerBase
{
    private readonly IDbService _dbService;

    public BattlePassController(IDbService dbService)
    {
        _dbService = dbService;
    }

    //get api/battlepass
    //возвращает массив всех лвлов батлпасса со статусом выполнения для текущего юзера
    [HttpGet]
    public async Task<IActionResult> GetMyBattlePass()
    {
        try
        {
            //извлечь юзерid 
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //если залоргнен то парсим его айди если нет то 0
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }

            var battlePass = await _dbService.GetUserBattlePassAsync(userId);

            return Ok(battlePass);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    //забираем награду за выполненный уровень (levelId допустим 4)
    [HttpPost("claim/{levelId}")]
    [Authorize]

    public async Task<IActionResult> ClaimReward(int levelId)
    {
        try
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _dbService.ClaimBattlePassRewardAsync(userId, levelId);

            if(!result.Success)
            {
                return BadRequest(new { Message = result.Message });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    //для текущего лвла
    [HttpGet("current-level")]
    public async Task<IActionResult> GetCurrentLevel([FromQuery] int? userId)
    {
        try
        {
            int targetUserId = 0;

            //если передан юзера айди то берем его значение(зырим чужой профапйл)
            if (userId.HasValue && userId.Value > 0)
            {
                targetUserId = userId.Value;
            }
            else
            {
                //если айди юзера не паередан значит зырим свой лвл

                //извлекаем userId из JWT токена
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                //проверка шо знач не пустое и конвенритуем тогда в int
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int.TryParse(userIdClaim, out targetUserId);
                }

            }

            //вызов метода из dbService колтоырй обращается к sql процедуре
            int currentLevel = await _dbService.GetUserCurrentBpLevelAsync(targetUserId);

            return Ok(new { CurrentLevel = currentLevel });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

}

