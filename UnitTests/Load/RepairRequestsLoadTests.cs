using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;

namespace UnitTests.Load;

/// <summary>
/// Нагрузочный сценарий для GET /api/repairrequests.
/// Эмулирует одновременную работу мастеров, которые регулярно открывают
/// список заявок на ремонт.
/// </summary>
public class RepairRequestsLoadTests
{
    [Fact]
    public void RepairRequests_List_Handles_Concurrent_Reads()
    {
        if (Environment.GetEnvironmentVariable("RUN_LOAD_TESTS") != "1")
            return;

        using var http = new HttpClient { BaseAddress = new Uri(LoadTestConfig.BaseUrl) };

        // Один раз получаем токен мастера — у этой роли есть доступ ко всем заявкам.
        var token = LoginAndGetToken(http, LoadTestConfig.Mechanic);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var scenario = Scenario.Create("repair_list", async context =>
            {
                var response = await http.GetAsync("/api/repairrequests");

                if (!response.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)response.StatusCode).ToString());

                var body  = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var success = doc.RootElement.GetProperty("success").GetBoolean();
                var isArray = doc.RootElement.GetProperty("data").ValueKind == JsonValueKind.Array;

                return success && isArray
                    ? Response.Ok(sizeBytes: body.Length)
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
            );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("loadtests")
            .WithTestName("repair_requests_list")
            .Run();

        var sc = stats.ScenarioStats[0];
        Assert.True(sc.Fail.Request.Percent < 1,
            $"Доля ошибок > 1%: {sc.Fail.Request.Percent}%");
        Assert.True(sc.Ok.Latency.Percent95 < 800,
            $"p95 latency > 800 мс: {sc.Ok.Latency.Percent95} мс");
    }

    private static string LoginAndGetToken(HttpClient http, LoadTestConfig.TestUser user)
    {
        var loginResp = http.PostAsJsonAsync("/api/auth/login", new
        {
            login    = user.Login,
            password = user.Password
        }).GetAwaiter().GetResult();

        loginResp.EnsureSuccessStatusCode();
        var body = loginResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("token").GetString()!;
    }
}
