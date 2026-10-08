using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using tastyDrop.Api.Controllers;
using tastyDrop.Api.Hubs;
using tastyDrop.Api.Services;
using tastyDrop.Api.Settings;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(); 
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddSignalR();//добавляем SignalR
//  Регистрируем DbService в контейнере Dependency Injection (DI)
// AddScoped означает: на каждый HTTP-запрос будет создаваться один
// экземпляр DbService и удаляться после ответа
builder.Services.AddScoped<IDbService, DbService>();

// Singleton один объект на всё время жизни сервера
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Singleton один объект на всё время жизни сервера
builder.Services.AddSingleton<IJwtService, JwtService>();

// Configure<SmtpSettings> связывает секцию "SmtpSettings" из appsettings.json с классом SmtpSettings.
// AddScoped<IEmailService, EmailService> — теперь EmailService можно внедрять в контроллеры.
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();


//настройка jwt авторизации 
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secrecKey = jwtSettings["SecretKey"]!;//достаем секрет кей


//настиройка аутентификаци
builder.Services.AddAuthentication(options =>
{
    //используем схему bearer по умолчанию
    //(пользователей через JWT‑токены, передаваемые в заголовке Authorization)
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => { //настраиваем правила проверки входящих jwt токенов
    options.TokenValidationParameters = new TokenValidationParameters // задаем правила проверки
    {
        ValidateIssuer = true, //проверять кто создал токен
        ValidateAudience = true, //проверять для кого предназначен токен
        ValidateLifetime = true, //проверять не истек ли срок дейтсивя 
        ValidateIssuerSigningKey = true, //проверять подпись с помощью секрет ключа
        ValidIssuer = jwtSettings["Issuer"], //валидный издатель
        ValidAudience = jwtSettings["Audience"], // валидный получатель
        //Сам секретный ключ для проверки
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secrecKey))
    };
    //Для signalR 
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Извлекаем токен из Query String
            var accessToken = context.Request.Query["access_token"];

            // Проверяем путь запроса (хаб CrashHub или LiveDropHub)
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/crash") || path.StartsWithSegments("/liveDropHub")))
            {
                // Передаем токен в контекст аутентификации
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});


//для работы swagger
builder.Services.AddEndpointsApiExplorer();


//настраиваем swagger чтоб в нем была кнопка authorize
builder.Services.AddSwaggerGen(c => {
    // Имя и версия API
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TastyDrop API", Version = "v1" });
    // Настраиваем форму ввода токена
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите Jwt токен как: Bearer YOUR_TOKEN", 
        Name = "Authorization", //название http- заголовка
        In = ParameterLocation.Header, // заголовок передаетсая в header
        Type = SecuritySchemeType.ApiKey, // тип API KEY
        Scheme = "Bearer"

    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement //применяем правило авторизакки ко всем эндпоинтам в swagger
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type =  ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    
    }); 
});

// 4 Настройка CORS (Cross-Origin Resource Sharing)
// Браузеры блокируют запросы, если Angular (localhost:4200) стучится на API (localhost:7xxx)
// Эта политика разрешает Angular свободно обращаться к нашему бэкенду
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Разрешаем только с этого адреса
                  .AllowAnyHeader() // Разрешаем любые заголовки
                  .AllowAnyMethod() // Разрешаем любые HTTP методы (GET, POST, PUT, DELETE и т.д.)
                  .AllowCredentials(); // Разрешаем отправку куки и авторизационных заголовков
        });
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Перенаправление с HTTP на защищенный HTTPS
app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication(); // Проверяем, кто пришел (распознаем JWT-токен)
app.UseAuthorization(); // проверяем есть ли доступ к запрошенному ресурку

//подключаем SignalR Hub
app.MapHub<LiveDropHub>("/liveDropHub");
//связываем url адреса с метоаными контроллеров 
app.MapControllers();
app.MapHub<CrashHub>("/hubs/crash"); // Маппинг конечной точки Hub

app.Run();
