using DAL.DBContext;
using DAL.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UnitTests.Integration;

/// <summary>
/// Интеграционные тесты для /api/repairrequests.
/// Используют WebApplicationFactory — поднимает настоящий ASP.NET Core pipeline
/// с InMemory-базой вместо SQL Server.
/// </summary>
public class RepairRequestsApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public RepairRequestsApiTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── GET /api/repairrequests — без токена → 401 ───────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/repairrequests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/auth/login — реальный логин через InMemory БД ─────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login    = "mechanic_test",
            password = "test1234"
        });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        var token = doc.RootElement
                       .GetProperty("data")
                       .GetProperty("token")
                       .GetString();
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns200WithSuccessFalse()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login    = "mechanic_test",
            password = "wrongpassword"
        });

        response.EnsureSuccessStatusCode(); // API возвращает 200 даже при ошибке
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // ── GET /api/repairrequests — с токеном механика → 200 ──────────────────

    [Fact]
    public async Task GetAll_WithMechanicToken_Returns200()
    {
        var client = _factory.CreateClient();
        var token  = await GetTokenAsync(client, "mechanic_test", "test1234");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/repairrequests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST /api/repairrequests — создание заявки студентом ─────────────────

    [Fact]
    public async Task CreateRequest_AsStudent_ReturnsCreatedRequest()
    {
        var client = _factory.CreateClient();
        var token  = await GetTokenAsync(client, "student_test", "test1234");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/repairrequests", new
        {
            title       = "Сломан стул",
            description = "Ножка отвалилась",
            blockId     = 1
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());

        // Статус новой заявки — Pending
        var data   = doc.RootElement.GetProperty("data");
        var status = data.GetProperty("status").GetString();
        Assert.Equal("Pending", status);
    }

    // ── PATCH /api/repairrequests/{id}/status ────────────────────────────────

    [Fact]
    public async Task UpdateStatus_AsMechanic_ChangesStatus()
    {
        var client = _factory.CreateClient();

        // Создаём заявку от студента
        var studentToken = await GetTokenAsync(client, "student_test", "test1234");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", studentToken);

        var createResp = await client.PostAsJsonAsync("/api/repairrequests", new
        {
            title   = "Течёт кран",
            blockId = 1
        });
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var requestId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        // Меняем статус от лица механика
        var mechanicToken = await GetTokenAsync(client, "mechanic_test", "test1234");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", mechanicToken);

        var patchResp = await client.PatchAsJsonAsync(
            $"/api/repairrequests/{requestId}/status",
            new { status = "InProgress", comment = "Взял в работу" });

        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);
        var patchJson = await patchResp.Content.ReadAsStringAsync();
        using var patchDoc = JsonDocument.Parse(patchJson);
        Assert.True(patchDoc.RootElement.GetProperty("success").GetBoolean());
    }

    // ── Вспомогательный: получить JWT-токен ─────────────────────────────────

    private static async Task<string> GetTokenAsync(
        HttpClient client, string login, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { login, password });
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty("data")
                  .GetProperty("token")
                  .GetString()!;
    }
}

/// <summary>
/// Фабрика тестового приложения: заменяет SQL Server на InMemory БД
/// и добавляет тестовых пользователей перед каждым запуском.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<WebAPI.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Удаляем регистрацию реального AppDbContext (SQL Server)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Регистрируем InMemory БД
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

            // Заполняем тестовыми данными
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        if (db.Roles.Any()) return; // уже засеяно

        // Роли
        var studentRole  = new Role { Id = 1, Name = "Student",  Description = "Студент" };
        var mechanicRole = new Role { Id = 4, Name = "Mechanic", Description = "Слесарь" };
        db.Roles.AddRange(studentRole, mechanicRole);

        // Блок (нужен для заявки на ремонт)
        var block = new Block { Id = 1, BlockNumber = "1", Floor = 1 };
        db.Blocks.Add(block);

        // SHA-256 хэш строки "test1234"
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("test1234")));

        // Тестовые пользователи
        db.Users.AddRange(
            new User
            {
                Id           = 1,
                FullName     = "Тестовый Студент",
                Username     = "student_test",
                Email        = "student@test.com",
                PasswordHash = hash,
                RoleId       = 1,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            },
            new User
            {
                Id           = 2,
                FullName     = "Тестовый Слесарь",
                Username     = "mechanic_test",
                Email        = "mechanic@test.com",
                PasswordHash = hash,
                RoleId       = 4,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            }
        );

        db.SaveChanges();
    }
}
