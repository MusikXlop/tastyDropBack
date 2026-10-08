namespace tastyDrop.Api.DTOs
{
    public class ResetPasswordDto
    {
        //почта польователя  
        public string Email { get; set; } = string.Empty;
        //нью код из письма
        public string Code { get; set; } = string.Empty;
        //новый пароь
        public string NewPassword { get; set; } = string.Empty;
    }
}
