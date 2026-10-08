using tastyDrop.Api.DTOs; // Подключаем DTO
using tastyDrop.Api.Services; // Подключаем сервисы
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Подключаем атрибуты и классы контроллеров .NET


namespace tastyDrop.Api.Controllers;

//
[ApiController]
[Route("api/[controller]")] //api/auth (настройка маршрут)
public class AuthController : ControllerBase
{
    private readonly IDbService _dbService; // поле для работы с бд
    private readonly IPasswordHasher _passwordHasher; //поле для хеширования
    private readonly IJwtService _jwtService; //поле для генерации JWT
    private readonly IEmailService _emailService; //email адрес


    // Внедряем все сервисы через конструктор
    public AuthController(
        IDbService dbService, 
        IPasswordHasher passwordHasher, 
        IJwtService jwtService,
        IEmailService emailService)
    {
        _dbService = dbService;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    //обрабатывает post запрос по адресу  /api/auth/register
    [HttpPost("register")]
    //данные беруется из тела json запроса
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            // Хешируем введенный пользователем пароль
            string passwordHash = _passwordHasher.HashPassword(dto.Password);

            //создаем уникальный  6-ти значгый код подтверждения
            string confirmCode = Random.Shared.Next(100000, 999999).ToString();

            //сохраняем в базу и получаем id
            int userId = await _dbService.RegisterUserAsync(dto, passwordHash, confirmCode);

            //отпраялем письмо с кодом на почту пользователя
            await _emailService.SendConfirmationCodeAsync(dto.Email, confirmCode);

            Console.WriteLine($"Регистрация: {dto.Username}, {dto.Email}");
            //возвращаем сообщение что код отправлен
            return Ok(new 
            {
                Message = "Регистрация успешна код подтверждения отправлен на твою почту",
                Email = dto.Email
            });

        }   
        catch (Exception ex) //если база вернет ошибку(пример почта занята
        {
            Console.WriteLine($"Ошибка регистрации: {ex.Message}");
            return BadRequest(new { Message = ex.Message });
        }
    }

    //POST /api/auth/confirm-email
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        try
        {
            //проверяем код в бд 
            var user = await _dbService.ConfirmEmailAsync(dto.Email, dto.Code);

            if (user == null)
            {
                return BadRequest(new { Message = "Неверный код чи Email" });
            }

            //выдаем jwt токен только после успешного подтверждения кода
            string token = _jwtService.GenerateToken(user.UserId, user.UserName, user.Role);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Username = user.UserName,
                Balance = user.Balance,
                UserId = user.UserId,
                BattlePassBalance = user.BattlePassBalance
            });
        
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }


    // Обрабатывает POST-запросы по адресу /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login ([FromBody] LoginDto dto)
    {
        //ищет юзера в бд по указанной почте 
        var user = await _dbService.GetUserByEmailAsync(dto.Email);



        //если пароль не найдет или не совпадает с шехем
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            //возвращаем ошибку
            Console.WriteLine($"Login: пользователь с Email={dto.Email} не найден");
            return BadRequest(new { Message = "Invalid ti ili password or email" });
        }
        else
        {
            Console.WriteLine($"Login: {user.UserEmail}, Confirmed={user.IsEmailConfirmed}");
        }
        //запрет вход если почта не подтверждена
        if(!user.IsEmailConfirmed)
        {
            string newCode = Random.Shared.Next(100000, 999999).ToString();

            //записываем новыйф код в бд 
            await _dbService.UpdateConfirmationCodeAsync(user.UserId, newCode);
            await _emailService.SendConfirmationCodeAsync(user.UserEmail, newCode);


            return BadRequest(new
            {
                RequiresConfirmation = true,
                Email = user.UserEmail,
                Message = "Подтверди свой email перед входом"
            });
        }


        //данные се фа ин (можно генерить токен)
        string token = _jwtService.GenerateToken(user.UserId, user.UserName, user.Role);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.UserName,
            Balance = user.Balance,
            UserId = user.UserId,
            BattlePassBalance = user.BattlePassBalance
        });
    }

    //пополнялка баланса
    [HttpPost("top-up")]
    [Authorize] //ток зареганным 
    public async Task<IActionResult> TopUpBalance([FromBody] TopUpDto dto)
    {
        if (dto.Amount <= 1 || dto.Amount > 2000)
        {
            return BadRequest(new { Message = "Сумма пополнения должна быть от 1 до 2000$" });
        }

        //получаем userId из JWT токена
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        //если токен не содержит корректный id то возвращаем ошибку 
        if(!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Неверный токен" });
        }

        try
        {
            //вызываем сервс в бд для обновления баланса 
            decimal newBalance = await _dbService.TopUpBalanceAsync(userId, dto.Amount);

            Console.WriteLine($"Пополнение: UserId={userId}, Amount={dto.Amount}, NewBalance={newBalance}");

            return Ok(new
            {
                message = "Баланс пополнен",
                NewBalance = newBalance
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var user = await _dbService.GetUserByEmailAsync(dto.Email);
            //если юзера нет то (не сообщаю об этом типа который восстанавливает акк
            //типо заглушка шоб не знали какая почта врегана а какая нет
            if(user == null)
            {
                return Ok(new { Message = "Если email существует код отправлен на почту." });
            }

            //генерируем нью код 
            string resetCode = Random.Shared.Next(100000, 999999).ToString();

            //записываем код в бд и отправляем письмо
            await _dbService.UpdateConfirmationCodeAsync(
                user.UserId, resetCode);

            //отправляем код юзера на почту 
            await _emailService.SendConfirmationCodeAsync(
                user.UserEmail, resetCode);

            return Ok(new { Message = "Код сброса пароля отправлен на твой Email" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }


    //нью пароль
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            //хешируем нью пароль
            string newPasswordHash =
                _passwordHasher.HashPassword(dto.NewPassword);

            //передаем в бд емаил код и нью хещ
            await _dbService.ResetPasswordAsync(
                dto.Email,
                dto.Code,
                newPasswordHash
            );

            return Ok(new { Message = "Пароль изменил вам иди входи в акк" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

}
