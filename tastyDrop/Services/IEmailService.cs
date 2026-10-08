namespace tastyDrop.Api.Services;
//интерфейс сервиса 
//определяет контракт (который реализуем в  EmailService)
public interface IEmailService
{
    //отправка кода подтверждения на почту
    Task SendConfirmationCodeAsync(string toEmail, string code);
}
