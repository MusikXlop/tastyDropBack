using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tastyDrop.Api.DTOs;
using tastyDrop.Api.Services;


namespace tastyDrop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Маршрут api/cases

    public class CasesController : ControllerBase
    {
        private readonly IDbService _dbService;

        public CasesController(IDbService dbService)// Внедряем сервис БД
        {
            _dbService = dbService;
        }


        //GET api/cases
        [HttpGet] // обработка GET запроса по адресу /api/cases
        public async Task<IActionResult> GetAll()
        {
            //получаем список всех кейсов 
            var cases = await _dbService.GetAllCasesAsync();
            return Ok(cases);//возврращаем массив кейсов (200 ок)
        }

        [HttpGet("recent-drops")] //  get запрос
        public async Task<IActionResult> GetRecentDrops()
        {
            //ласт 25 выбитых шмоток из LiveDrops для инициализации ленты на фронтенде
            var recentDrops = await _dbService.GetRecentDropsAsync();
            return Ok(recentDrops);
        }


        //get api/cases/1/items
        [HttpGet("{id}/items")] //обрабатывает get запрос с id кейса в url
        // id достаем из url
        public async Task<IActionResult> GetCaseItems(int id)
        {
            //достаем список шмоток в этом кейсе
            var items = await _dbService.GetCaseItemsAsync(id);
            return Ok(items); //если все норм то ок 200
        }


        //get freecase
        [HttpGet("{caseId}/free-status")]
        [Authorize]
        public async Task<IActionResult> CheckFreeStatus(int caseId)
        {
            try
            {
                // Достаем UserId из JWT токена

                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                bool isAvailable = await _dbService.CheckUserFreeCaseStatusAsync(userId, caseId);

                return Ok(new FreeCaseStatusDto
                {
                    IsFreeAvailable = isAvailable,
                    Reason = isAvailable ? "Бесплатное " : "Плаьно "
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
