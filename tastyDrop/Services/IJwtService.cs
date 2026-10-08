namespace tastyDrop.Api.Services;

    //интерфейс для jwt сервиса
    public interface IJwtService
    {
        // Метод создания токена, принимающий данные юзера
        string GenerateToken(int userId, string username, string role);
    
        
    
    }

