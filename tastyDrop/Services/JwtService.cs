using System.IdentityModel.Tokens.Jwt; // Импортируем классы для генерации JWT-токенов
using System.Security.Claims; // Импортируем Claim (утверждения/данные о пользователе внутри токена)
using System.Text; // Импортируем Encoding для перевода строк в байты
using Microsoft.IdentityModel.Tokens; // Импортируем ключи шифрования и алгоритмы подписи

namespace tastyDrop.Api.Services;

public class JwtService : IJwtService
{
    //закрытое поле для доступа к настройкам (appsettings.json)
    private readonly IConfiguration _config;

    //конструктор для внедления зависимости (di) получаем конфигурацию
    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    //главный метод генерации
    public string GenerateToken(int userId, string username, string role)
    {
        //считывает секцию JwtSettings из appsettings.json
        var jwtSettings = _config.GetSection("JwtSettings");

        //достаем секретный ключ 
        // ! гарант что ключ там есть 
        var secretKey = jwtSettings["SecretKey"]!;

        //создаем массив данных которые будут зашиты внутрь токена 
        var claims = new[]
        {
            //создаем id юзера
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username), //сохраняем имя пользователя
            new Claim(ClaimTypes.Role, role) //сохраняем роль пользователя(user or admin)
         };

        //превращаем секретный ключ в массив байтов для криптографии
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        //создаем цифровую подписать на основе  .HmacSha256
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(//собираем объект токена из деталей
            issuer: jwtSettings["Issuer"], //кто выдал токен(наш сервак)
            audience: jwtSettings["Audience"], //для кого токен (наш фронт)
            claims: claims, //данные о юзере
            expires: DateTime.UtcNow.AddDays(7), // срок жизнь токена 7 дней
            signingCredentials: creds //подпись токена
        );

        //превращаем объект токена в итоговую длинную строку "sdfap[kosdlfkasfda"
        return new JwtSecurityTokenHandler().WriteToken(token); 
    }
}
