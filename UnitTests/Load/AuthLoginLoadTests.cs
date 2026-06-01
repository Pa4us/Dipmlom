using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace UnitTests.Load;

/// <summary>
/// Нагрузочный сценарий для POST /api/auth/login.
///
/// Запуск: dotnet test --filter "FullyQualifiedName~AuthLoginLoadTests"
/// или    dotnet run --project UnitTests -c Release -- nbomber (при отдельной точке входа).
///
/// По умолчанию запуск помечен явным флагом окружения, чтобы случайно не
/// триггериться в обычном прогоне unit-тестов.
/// </summary>
public class AuthLoginLoadTests
{
    [Fact]
    public void Login_Endpoint_Handles_Concurrent_Users()
    {
        if (Environment.GetEnvironmentVariable("RUN_LOAD_TESTS") != "1")
        {
            // По умолчанию нагрузочные тесты пропускаются — слишком тяжёлые для CI.
            return;
        }

        using var http = new HttpClient { BaseAddress = new Uri(LoadTestConfig.BaseUrl) };

        var scenario = Scenario.Create("auth_login", async context =>
            {
                // Распределяем нагрузку по всем пяти тестовым ролям по кругу.
                var user = LoadTestConfig.AllUsers[(int)(context.InvocationNumber % LoadTestConfig.AllUsers.Length)];

                var response = await http.PostAsJsonAsync("/api/auth/login", new
                {
                    login    = user.Login,
                    password = user.Password
                });

                if (!response.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)response.StatusCode).ToString());

                var body  = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var success = doc.RootElement.GetProperty("success").GetBoolean();
                var token   = doc.RootElement.GetProperty("data").GetProperty("token").GetString();

                return success && !string.IsNullOrEmpty(token)
                    ? Response.Ok(sizeBytes: body.Length)
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                // Раскачка: до 20 одновременных юзеров за 30 с
                Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                // Удержание нагрузки 1 минуту
                Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
            );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("loadtests")
            .WithTestName("auth_login")
            .Run();

        // Пороги для прохождения теста
        var sc = stats.ScenarioStats[0];
        Assert.True(sc.Fail.Request.Percent < 1,
            $"Доля ошибок > 1%: {sc.Fail.Request.Percent}%");
        Assert.True(sc.Ok.Latency.Percent95 < 800,
            $"p95 latency > 800 мс: {sc.Ok.Latency.Percent95} мс");
    }
}
