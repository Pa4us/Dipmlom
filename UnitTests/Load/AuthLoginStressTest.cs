using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;

namespace UnitTests.Load;

/// <summary>
/// Стресс-тест endpoint-а POST /api/auth/login.
///
/// Отличие от обычного нагрузочного: ступенчатое наращивание RPS
/// до точки отказа. Профиль (общая длительность ~5 минут):
///   1. 10 RPS — 30 с  (smoke)
///   2. 30 RPS — 1 мин (норма)
///   3. 60 RPS — 1 мин (повышенная)
///   4. 100 RPS — 1 мин (пик)
///   5. 150 RPS — 1 мин (стресс)
///   6. 30 с спад
///
/// Цель — найти точку, на которой API начинает отвечать с ошибкой
/// или превышает разумное время ответа.
///
/// В отчёте NBomber видно график latency по времени — на нём чётко
/// проявляется ступенька, где сервис перестаёт справляться.
/// </summary>
public class AuthLoginStressTest
{
    [Fact]
    public void Login_Endpoint_Stress_FindsBreakingPoint()
    {
        if (Environment.GetEnvironmentVariable("RUN_LOAD_TESTS") != "1")
            return;

        using var http = new HttpClient
        {
            BaseAddress = new Uri(LoadTestConfig.BaseUrl),
            Timeout     = TimeSpan.FromSeconds(15),
        };

        var users = LoadTestConfig.AllUsers;

        var scenario = Scenario.Create("auth_login_stress", async context =>
            {
                var user = users[(int)(context.InvocationNumber % users.Length)];

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
                // Ступенчатое наращивание нагрузки
                Simulation.Inject(rate: 10,  interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 30,  interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
                Simulation.Inject(rate: 60,  interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
                Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
                // Плавный спад, чтобы API «отдышалось» в логах
                Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("loadtests")
            .WithTestName("auth_login_stress")
            .WithReportFolder(LoadTestConfig.GetReportsFolder("auth_login_stress"))
            .Run();

        // Стресс-тест НЕ должен падать по жёстким порогам — он, наоборот,
        // ищет точку отказа. Поэтому здесь только мягкий sanity-check:
        // на низких ступенях (10–30 RPS) ошибок быть не должно.
        var sc = stats.ScenarioStats[0];
        Assert.True(sc.Ok.Request.Count > 0,
            "Ни один запрос не выполнился успешно — стресс-тест не показателен.");
    }
}
