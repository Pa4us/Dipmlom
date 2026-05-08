namespace WebAPI.Services;

public interface IPasswordResetService
{
    /// <summary>Генерирует токен для email и сохраняет его (30 мин).</summary>
    string GenerateToken(string email);

    /// <summary>Возвращает email по токену или null если токен истёк/не найден.</summary>
    string? GetEmailByToken(string token);

    /// <summary>Удаляет токен после успешного сброса пароля.</summary>
    void InvalidateToken(string token);
}
