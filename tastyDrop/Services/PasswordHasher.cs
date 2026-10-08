namespace tastyDrop.Api.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Реализация хэширования пароля
            // он берет пароль сам добавляет случайную «соль» и превращает в хеш
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Реализация метода проверки
        public bool VerifyPassword(string password, string passwordHash)
        {
            // BCrypt сравнивает пароль с хешем и возвращает true (совпал) или false (не совпал)
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
