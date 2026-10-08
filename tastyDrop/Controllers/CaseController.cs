using System.Security.Claims; // Для чтения Claims из токена
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Для атрибута [Authorize]
using tastyDrop.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace tastyDrop.Api.Controllers;

[ApiController]
[Route("api/[controller]")] //формирует URL api/case
public class CaseController : ControllerBase
{
    private readonly IDbService _dbService;

    private readonly IHubContext<LiveDropHub> _hubContext;// для работы с SignalR

    //через конструктор получаем наш серсис работы 
    //с бд  

    public CaseController(IDbService dbService, IHubContext<LiveDropHub> hubContext)
    {
        _dbService = dbService;
        _hubContext = hubContext;
    }

    //POST запрос по адрусу https://localhost:xxxx/api/case/open
    [HttpPost("open")]
    [Authorize] // открыть кейс может только авторизированный юзер

    public async Task<IActionResult> OpenCase([FromBody] OpenCaseDto request)
    {
        try
        {

            //безопасно получаем id текущенго игрока из расщифрованного jwt токена
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string username = User.FindFirstValue(ClaimTypes.Name) ?? "Бедолага";

            //[FromBody] говорит asp.net распарсить json из тела запроса
            // в объекь OpenCaseDto



            //вызыаваем логику опенкейса в бд 
            var (wonItems, newBalance, newBpBalance) = await _dbService.OpenCaseAsync(userId, request.CaseId, request.Amount);

            //достаем инфу о кейса для отображения в лайв дропе
            var allCases = await _dbService.GetAllCasesAsync();
            var currentCase = allCases.FirstOrDefault(c => c.CaseId == request.CaseId);
            
            //отправляка каждый выпавший предмет в ленту 
            foreach (var item in wonItems)
            {

                //создаем строгую типизированный dto 
                var liveDropDto = new LiveDropDto
                {
                    DropId = item.DropId,
                    UserId = userId,
                    Username = username,
                    ItemName = item.Name,
                    ItemImage = item.ImageUrl,
                    ItemRarity = item.Rarity,
                    CaseId = request.CaseId,
                    CaseName = currentCase?.Name ?? "Кейс",
                    CaseImage = currentCase?.ImageUrl ?? "",
                    DropTime = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("ReceiveDrop", liveDropDto);
                
            }    


            //возвращаем HTTP STATUS 200 = OK И JSON объект с выигрышем и балансом
            return Ok(new
            {
                Success = true,
                WonItemDto = wonItems,
                NewBalance = newBalance,
                NewBattlePassBalance = newBpBalance

            });
        }
        catch (Exception ex)
        {
            //если в бд ошибка то ловим и отдаем клиенту статут 404
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
