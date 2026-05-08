using SharedModel.DTOs.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebAPP.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    /// <summary>
    /// Возвращает Authorization-заголовок из сессии.
    /// Используется per-request, а не через DefaultRequestHeaders
    /// (DefaultRequestHeaders — не потокобезопасны при параллельных запросах).
    /// </summary>
    private AuthenticationHeaderValue? GetAuthHeader()
    {
        var token = _accessor.HttpContext?.Session.GetString("JWT");
        return string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = GetAuthHeader();
        if (content != null)
            req.Content = content;
        return req;
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string url)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, url);
            var resp = await _http.SendAsync(req);
            return await Read<T>(resp);
        }
        catch { return null; }
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string url, object body)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
            using var req = BuildRequest(HttpMethod.Post, url, content);
            var resp = await _http.SendAsync(req);
            // Читаем тело даже при ошибочных статусах (4xx, 5xx) — там JSON с Message
            return await Read<T>(resp);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Ошибка соединения с сервером: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>?> PatchAsync<T>(string url, object body)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
            using var req = BuildRequest(HttpMethod.Patch, url, content);
            var resp = await _http.SendAsync(req);
            return await Read<T>(resp);
        }
        catch { return null; }
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string url, object body)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
            using var req = BuildRequest(HttpMethod.Put, url, content);
            var resp = await _http.SendAsync(req);
            return await Read<T>(resp);
        }
        catch { return null; }
    }

    public async Task<ApiResponse<T>?> DeleteAsync<T>(string url)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Delete, url);
            var resp = await _http.SendAsync(req);
            return await Read<T>(resp);
        }
        catch { return null; }
    }

    private static async Task<ApiResponse<T>?> Read<T>(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOpts);
    }
}
