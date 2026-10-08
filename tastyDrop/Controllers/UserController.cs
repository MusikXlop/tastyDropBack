using System.Security.Claims;
using tastyDrop.Api.Services;
using Microsoft.AspNetCore.Authorization; // Подключаем атрибуты защиты
using Microsoft.AspNetCore.Mvc;
using tastyDrop.Api.DTOs;

namespace tastyDrop.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")] //Маршрут: api/user

    public class UserController : ControllerBase
    {
        private readonly IDbService _dbService;

        public UserController(IDbService dbService)
        {
            _dbService = dbService;
        }

        // GET запрос /api/user/inventory
        [HttpGet("inventory")]
        // защита энлпоинта (доступ ток с валдиным jwt токеном в заголовке 
        [Authorize] 
        public async Task<IActionResult> GetInventory()
        {

            //извлекаем айди пользователя прямо из токена запроса
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Получаем его инвентарь из базы
            var inventory = await _dbService.GetUserInventoryAsync(userId);

            return Ok(inventory); // отправляем скисок выбитых предметов
        }

        //разрешаем зірить чужие профайлы
        [HttpGet("inventory/{userId}")]
        public async Task<IActionResult> GetUserInventory(int userId)
        {

            var inventory = await _dbService.GetUserInventoryAsync(userId);
            return Ok(inventory);
        }

        [HttpGet("{userId}/username")]
        public async Task<IActionResult> GetUsername(int userId)
        {
            var username = await _dbService.GetUsernameByIdAsync(userId);
            if (username == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            return Ok(new { username });
        }

        //бест дроп
        [HttpGet("best-drop")]
        [Authorize]
        public async Task<IActionResult> GetMyBestDrop()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var bestDrop = await _dbService.GetUserBestDropAsync(userId);

            if (bestDrop == null)
            {
                return Ok(new { Message = "лучшего Дропа пока нема" });
            }
            return Ok(bestDrop);
        }

        //для просмотра чужого дропа бест
        [HttpGet("{userId}/best-drop")]
        public async Task<IActionResult> GetUserBestDrop(int userId)
        {
            var bestDrop = await _dbService.GetUserBestDropAsync(userId);

            if(bestDrop == null)
            {
                return Ok(null);
            }
            return Ok(bestDrop);
           
        }

    }

}
