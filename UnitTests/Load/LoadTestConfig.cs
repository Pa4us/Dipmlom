namespace UnitTests.Load;

/// <summary>
/// Общая конфигурация для нагрузочных сценариев NBomber.
/// Адрес API можно переопределить через переменную окружения LOADTEST_BASE_URL.
/// Тестовые пользователи должны существовать в БД (см. seed_data.sql).
/// </summary>
internal static class LoadTestConfig
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("LOADTEST_BASE_URL") ?? "http://localhost:5229";

    public record TestUser(string Login, string Password);

    public static readonly TestUser Student   = new("student_test",   "test1234");
    public static readonly TestUser Inspector = new("inspector_test", "test1234");
    public static readonly TestUser Educator  = new("educator_test",  "test1234");
    public static readonly TestUser Manager   = new("manager_test",   "test1234");
    public static readonly TestUser Mechanic  = new("mechanic_test",  "test1234");

    public static readonly TestUser[] AllUsers =
        { Student, Inspector, Educator, Manager, Mechanic };
}
