using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;

namespace UnitTests.Load;

/// <summary>
/// Поиск точки отказа endpoint-а POST /api/auth/login.
///
/// В отличие от обычного стресс-теста с фиксированным потолком (150 RPS),
/// этот тест последовательно прогоняет нагрузку с растущей интенсивностью:
/// 200 → 400 → 700 → 1000 → 1500 → 2000 → 3000 → 5000 запросов в секунду.
///
/// Каждая ступень — отдельный запуск NBomber на 20 секунд. После каждой
/// ступени анализируется доля ошибок и p95 latency. Тест прекращается
/// при достижении любого из условий:
///   • доля ошибок &gt; 5%
///   • p95 latency &gt; 2 000 мс
///
/// На этой ступени и фиксируется точка отказа API.
/// </summary>
public class AuthLoginBreakingPointTest
{
    // Пороги, по которым нагрузка считается "сломавшей" API
    private const double MaxFailPercent       = 5.0;
    private const double MaxP95LatencyMs      = 2000.0;

    [Fact]
    public void Login_BreakingPoint_FindsExactRpsLimit()
    {
        if (Environment.GetEnvironmentVariable("RUN_LOAD_TESTS") != "1")
            return;

        using var http = new HttpClient
        {
            BaseAddress = new Uri(LoadTestConfig.BaseUrl),
            Timeout     = TimeSpan.FromSeconds(15),
        };

        var users = LoadTestConfig.AllUsers;
        var rates = new[] { 200, 400, 700, 1000, 1500, 2000, 3000, 5000 };

        int? breakingRps = null;
        var  summary     = new List<(int rps, long ok, long fail, double failPct, double p95)>();

        Console.WriteLine();
        Console.WriteLine("=== ПОИСК ТОЧКИ ОТКАЗА API ===");

        foreach (var rate in rates)
        {
            Console.WriteLine();
            Console.WriteLine($"--- Ступень: {rate} RPS на 20 секунд ---");

            var scenario = Scenario.Create($"login_{rate}rps", async context =>
                {
                    var user = users[(int)(context.InvocationNumber % users.Length)];

                    try
                    {
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

                        return success
                            ? Response.Ok(sizeBytes: body.Length)
                            : Response.Fail();
                    }
                    catch (Exception ex)
                    {
                        return Response.Fail(message: ex.GetType().Name);
                    }
                })
                .WithoutWarmUp()
                .WithLoadSimulations(
                    Simulation.Inject(
                        rate:     rate,
                        interval: TimeSpan.FromSeconds(1),
                        during:   TimeSpan.FromSeconds(20))
                );

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithTestSuite("loadtests")
                .WithTestName($"breaking_point_{rate}rps")
                .WithReportFolder(LoadTestConfig.GetReportsFolder($"breaking_{rate}rps"))
                .Run();

            var sc       = stats.ScenarioStats[0];
            var ok       = sc.Ok.Request.Count;
            var fail     = sc.Fail.Request.Count;
            var total    = ok + fail;
            var failPct  = total > 0 ? (double)fail / total * 100 : 0;
            var p95      = sc.Ok.Latency.Percent95;

            summary.Add((rate, ok, fail, failPct, p95));

            Console.WriteLine($"   ok={ok}, fail={fail} ({failPct:F1}%), p95={p95:F0}ms");

            if (failPct > MaxFailPercent || p95 > MaxP95LatencyMs)
            {
                breakingRps = rate;
                break;
            }
        }

        // Итоговая сводка по всем ступеням
        Console.WriteLine();
        Console.WriteLine("=== ИТОГОВАЯ СВОДКА ===");
        Console.WriteLine("  RPS    |   ok   |  fail  | fail%  |  p95(ms)");
        Console.WriteLine("  -------+--------+--------+--------+---------");
        foreach (var (rps, ok, fail, failPct, p95) in summary)
            Console.WriteLine($"  {rps,5}  | {ok,6} | {fail,6} | {failPct,5:F1}% | {p95,7:F0}");
        Console.WriteLine();

        if (breakingRps != null)
        {
            Console.WriteLine($"*** API СЛОМАЛОСЬ НА НАГРУЗКЕ: {breakingRps} RPS ***");
            Console.WriteLine($"    Порог отказа: >{MaxFailPercent}% ошибок ИЛИ p95 latency >{MaxP95LatencyMs} мс");
        }
        else
        {
            Console.WriteLine($"*** Точка отказа НЕ достигнута даже на {rates.Last()} RPS ***");
            Console.WriteLine($"    API устойчив ко всему диапазону нагрузок.");
        }

        // Тест считается пройденным, если он успешно прогнал хотя бы одну ступень
        Assert.True(summary.Count > 0, "Ни одной ступени нагрузки не выполнено.");
    }
}
