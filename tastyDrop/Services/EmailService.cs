using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

using tastyDrop.Api.Settings;

namespace tastyDrop.Api.Services;

public class EmailService : IEmailService
{
    //объект с настройками
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        //полчаем знапчения из конфигурации
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendConfirmationCodeAsync(string toEmail, string code)
    {
        try
        {
            Console.WriteLine($"→ Отправка письма: {toEmail}, Code={code}");
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName),
                Subject = "Код подтверждения",
                Body = $@"
                <div style=""font-family: Arial, sans-serif; padding: 20px;"">
                    <h2>Подтверждение регистрации</h2>
                    <p>На самом деле это рандом код вот настоящий: 1 5 4 3 3 7 </p>
                    <h1 style=""color: #2b7fff; letter-spacing: 5px;"">{code}</h1>
                    <p>Если вы не регистрировались на нашем сайте, просто идите регаться.</p>
                </div>",
                IsBodyHtml = true// тело письма в HTML
            };
            mailMessage.To.Add(toEmail); //кому отправляем 

            //создаем smtp клиент (протокол отправки писем)
            using var client = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
            {
                //логин + пароль
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                // включаем шифрование
                EnableSsl = true
            };
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка SMTP: {ex.Message}");
            throw;
        }
    }

}
