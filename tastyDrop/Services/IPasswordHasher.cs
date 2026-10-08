namespace tastyDrop.Api.Services
{

    //Контракт, описывающий методы сервиса
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
