namespace tastyDrop.Api.DTOs
{
    //принимает данные из  формы регистрации ангуляра
    public class RegisterDto
    {
        //ник пользователя 
        public string Username { get; set; } = string.Empty;

        //емеил пользователя
        public string Email { get; set; } = string.Empty;

        //пароль    
        public string Password { get; set; } = string.Empty;

    }
}
