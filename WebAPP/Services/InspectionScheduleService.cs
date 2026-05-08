using System.Text.Json;

namespace WebAPP.Services;

/// <summary>
/// Singleton-сервис для хранения расписания дней инспекций.
/// Воспитатель выбирает дни недели (0=Пн … 6=Вс), в которые инспекторам
/// разрешено проводить проверки. Данные хранятся в JSON-файле.
/// </summary>
public class InspectionScheduleService
{
    private readonly string _filePath;
    private HashSet<int> _enabledDays; // 0=Monday … 6=Sunday (DayOfWeek - 1, с учётом Пн=0)

    public static readonly string[] DayNames =
    {
        "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"
    };

    public InspectionScheduleService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "inspection_schedule.json");
        _enabledDays = LoadFromFile();
    }

    /// <summary>Доступные дни (0=Пн … 6=Вс)</summary>
    public IReadOnlySet<int> EnabledDays => _enabledDays;

    /// <summary>Разрешена ли проверка сегодня?</summary>
    public bool IsInspectionAllowedToday()
    {
        // DayOfWeek: Sunday=0, Monday=1 … Saturday=6
        // Преобразуем в 0=Monday … 6=Sunday
        var dow = (int)DateTime.Today.DayOfWeek;
        var normalized = dow == 0 ? 6 : dow - 1; // Sunday → 6, Mon → 0 … Sat → 5
        return _enabledDays.Contains(normalized);
    }

    /// <summary>Обновить расписание (вызывается воспитателем)</summary>
    public void UpdateSchedule(IEnumerable<int> days)
    {
        _enabledDays = days.Where(d => d >= 0 && d <= 6).ToHashSet();
        SaveToFile();
    }

    private HashSet<int> LoadFromFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var days = JsonSerializer.Deserialize<int[]>(json);
                if (days != null)
                    return new HashSet<int>(days);
            }
        }
        catch { /* ignore */ }

        // По умолчанию: понедельник, среда, пятница (0, 2, 4)
        return new HashSet<int> { 0, 2, 4 };
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_enabledDays.ToArray());
            File.WriteAllText(_filePath, json);
        }
        catch { /* ignore */ }
    }
}
