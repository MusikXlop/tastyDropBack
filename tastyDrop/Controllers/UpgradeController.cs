namespace tastyDrop.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Channels;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;
using Microsoft.AspNetCore.SignalR;
using tastyDrop.Api.Hubs;



// Помечаем класс как API-контроллер
// и включаем специальные правила ASP.NET Core
// для привязки параметров валидации моделей
// и автоматического ответа 400 при ошибках валидации
[ApiController]

//указываем базовый маршррут контролера 
[Route("api/[controller]")]

//надо авторизацию
[Authorize]

public class UpgradeController : ControllerBase
{
    //ссылка для работы в бд 
    private readonly IDbService _dbService;

    //ссылка на signalR для отправки дропов всем подключенным пользователям
    private readonly IHubContext<LiveDropHub> _hubContext;

    //конструктор dp
    public UpgradeController(
        IDbService dbService, IHubContext<LiveDropHub> hubContext)
    {
        _dbService = dbService;

        _hubContext = hubContext;
    }

    //маршрут run 
    [HttpPost("run")]

    //обработка апгрейда
    public async Task<IActionResult> RunUpgrade([FromBody] UpgradeRequestDto dto)
    {
        //получаем айди пользователя из claims авторизованного пользователя
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //проверка есть ли claimn и можно ли его переобразовать в int
        if(string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            //если пользователь не авторизовал или id некорректный то возврат ошибка
            return Unauthorized(new { Message = "Пользователь не авторизован" });

        }

        try
        {
            //запускаем апгрейд через дб сервис
            var result = await _dbService.ExecuteUpgradeAsync(userId, dto.InventoryId, dto.TargetItemId);
            
            //если апгрейд успешный то отправляем дроп в ленту всем 
            if(result.IsSuccess && result.WonItem != null)
            {
                //получаем username текущего польователя из jwt
                string username = User.FindFirstValue(ClaimTypes.Name)
                                  ?? User.FindFirstValue("username")
                                  ?? "Бедолага";

                //создаем объект livedropdto специально для ленты
                var liveDropDto = new LiveDropDto
                {
                    DropId = result.WonItem.DropId,

                    UserId = userId,

                    Username = username,

                    ItemName = result.WonItem.Name,

                    ItemImage = result.WonItem.ImageUrl,

                    ItemRarity = result.WonItem.Rarity,

                    //upgrade не относится к кесйам
                    CaseId = null,

                    CaseName = "DownGrade",

                    CaseImage = "",

                    DropTime = DateTime.UtcNow
                };

                //отправляем событие ReceiveDrop всем юзерам
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveDrop",
                    liveDropDto
                );

            }


            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }


    }

}
