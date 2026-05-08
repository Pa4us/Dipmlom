using Microsoft.Extensions.Caching.Memory;

namespace WebAPI.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public PasswordResetService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateToken(string email)
    {
        // Инвалидируем старый токен, если есть
        if (_cache.TryGetValue(EmailKey(email), out string? oldToken) && oldToken != null)
            _cache.Remove(TokenKey(oldToken));

        var token = Guid.NewGuid().ToString("N"); // 32-символьный hex без дефисов
        var opts  = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenLifetime };

        _cache.Set(TokenKey(token), email, opts);
        _cache.Set(EmailKey(email), token, opts);

        return token;
    }

    public string? GetEmailByToken(string token)
    {
        return _cache.TryGetValue(TokenKey(token), out string? email) ? email : null;
    }

    public void InvalidateToken(string token)
    {
        if (_cache.TryGetValue(TokenKey(token), out string? email) && email != null)
            _cache.Remove(EmailKey(email));
        _cache.Remove(TokenKey(token));
    }

    private static string TokenKey(string token) => $"pwreset:token:{token}";
    private static string EmailKey(string email) => $"pwreset:email:{email}";
}
