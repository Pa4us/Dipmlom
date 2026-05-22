using System.Text.Json;

namespace WebAPP.Services;

public class InspectionScheduleService
{
    private readonly string _filePath;
    private int _selectedDay; // 0=Пн … 6=Вс

    public static readonly string[] DayNames =
    {
        "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"
    };

    public static readonly string[] DayShortNames =
    {
        "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"
    };

    public InspectionScheduleService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "inspection_schedule.json");
        _selectedDay = LoadFromFile();
    }

    public int SelectedDay => _selectedDay;

    public bool IsInspectionAllowedToday()
    {
        var dow = (int)DateTime.Today.DayOfWeek;
        var normalized = dow == 0 ? 6 : dow - 1;
        return normalized == _selectedDay;
    }

    public void UpdateSchedule(int day)
    {
        if (day >= 0 && day <= 6)
        {
            _selectedDay = day;
            SaveToFile();
        }
    }

    public List<DateOnly> GetMonthSchedule(int? year = null, int? month = null)
    {
        var now = DateTime.Today;
        int y = year ?? now.Year;
        int m = month ?? now.Month;
        var dates = new List<DateOnly>();
        int daysInMonth = DateTime.DaysInMonth(y, m);
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(y, m, d);
            var dow = (int)date.DayOfWeek;
            var normalized = dow == 0 ? 6 : dow - 1;
            if (normalized == _selectedDay)
                dates.Add(date);
        }
        return dates;
    }

    private int LoadFromFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<ScheduleData>(json);
                if (data != null)
                    return Math.Clamp(data.SelectedDay, 0, 6);

                var legacy = JsonSerializer.Deserialize<int[]>(json);
                if (legacy != null && legacy.Length > 0)
                    return Math.Clamp(legacy[0], 0, 6);
            }
        }
        catch { /* ignore */ }

        return 2; // По умолчанию: среда (0=Пн, 2=Ср)
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(new ScheduleData { SelectedDay = _selectedDay });
            File.WriteAllText(_filePath, json);
        }
        catch { /* ignore */ }
    }

    private class ScheduleData
    {
        public int SelectedDay { get; set; }
    }
}
