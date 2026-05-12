using ClosedXML.Excel;
using SharedModel.DTOs;

namespace WebAPP.Services;

/// <summary>
/// Парсит загруженные Excel-файлы в промежуточные DTO для последующей валидации через API.
/// </summary>
public static class ExcelImportService
{
    // ─── Импорт аккаунтов ────────────────────────────────────────────────────
    // Ожидаемые заголовки (регистр не важен):
    //   ФИО | Логин | Email | Роль

    public static (List<ImportUserRowDto> rows, string? error) ParseUsersFile(Stream stream)
    {
        try
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            // Ищем строку с заголовками (первая непустая)
            var headerRow = FindHeaderRow(ws, new[] { "фио", "логин", "email" });
            if (headerRow == 0)
                return (new(), "Не найдена строка заголовков. Убедитесь, что файл содержит колонки: ФИО, Логин, Email, Роль");

            var map = BuildColumnMap(ws.Row(headerRow));
            if (!map.ContainsKey("фио"))    return (new(), "Не найдена колонка «ФИО»");
            if (!map.ContainsKey("логин"))  return (new(), "Не найдена колонка «Логин»");
            if (!map.ContainsKey("email"))  return (new(), "Не найдена колонка «Email»");

            var rows  = new List<ImportUserRowDto>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (IsRowEmpty(row)) continue;

                var fullName = GetCell(row, map, "фио");
                var username = GetCell(row, map, "логин");
                var email    = GetCell(row, map, "email");
                var role     = map.ContainsKey("роль") ? GetCell(row, map, "роль") : "Student";

                rows.Add(new ImportUserRowDto
                {
                    RowNumber = r,
                    FullName  = fullName,
                    Username  = username,
                    Email     = email,
                    RoleName  = string.IsNullOrWhiteSpace(role) ? "Student" : role.Trim(),
                });
            }

            if (rows.Count == 0)
                return (new(), "Файл не содержит строк с данными");

            return (rows, null);
        }
        catch (Exception ex)
        {
            return (new(), $"Не удалось прочитать файл: {ex.Message}");
        }
    }

    // ─── Импорт заселения ────────────────────────────────────────────────────
    // Ожидаемые заголовки:
    //   Логин | ФИО (необяз.) | Блок | Комната (необяз.)

    public static (List<ImportResidenceRowDto> rows, string? error) ParseResidencesFile(Stream stream)
    {
        try
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            var headerRow = FindHeaderRow(ws, new[] { "логин", "блок" });
            if (headerRow == 0)
                return (new(), "Не найдена строка заголовков. Убедитесь, что файл содержит колонки: Логин, Блок");

            var map = BuildColumnMap(ws.Row(headerRow));
            if (!map.ContainsKey("логин")) return (new(), "Не найдена колонка «Логин»");
            if (!map.ContainsKey("блок"))  return (new(), "Не найдена колонка «Блок»");

            var rows    = new List<ImportResidenceRowDto>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (IsRowEmpty(row)) continue;

                var blockRaw  = GetCell(row, map, "блок");
                var roomRaw   = map.ContainsKey("комната") ? GetCell(row, map, "комната") : null;

                // Поддержка формата "44-1" в колонке Блок:
                // если значение содержит "-", то часть до "-" = номер блока,
                // часть после "-" = номер комнаты (если колонка Комната не задана отдельно)
                string blockNumber;
                string? roomNumber;
                var dashIdx = blockRaw.IndexOf('-');
                if (dashIdx > 0 && string.IsNullOrWhiteSpace(roomRaw))
                {
                    blockNumber = blockRaw[..dashIdx].Trim();
                    roomNumber  = blockRaw[(dashIdx + 1)..].Trim();
                }
                else
                {
                    blockNumber = blockRaw;
                    roomNumber  = string.IsNullOrWhiteSpace(roomRaw) ? null : roomRaw;
                }

                rows.Add(new ImportResidenceRowDto
                {
                    RowNumber   = r,
                    Username    = GetCell(row, map, "логин"),
                    FullName    = map.ContainsKey("фио") ? GetCell(row, map, "фио") : null,
                    BlockNumber = blockNumber,
                    RoomNumber  = roomNumber,
                });
            }

            if (rows.Count == 0)
                return (new(), "Файл не содержит строк с данными");

            return (rows, null);
        }
        catch (Exception ex)
        {
            return (new(), $"Не удалось прочитать файл: {ex.Message}");
        }
    }

    // ─── Вспомогательные методы ───────────────────────────────────────────────

    /// <summary>Ищет первую строку где хотя бы <paramref name="requiredKeys"/> встречаются в ячейках.</summary>
    private static int FindHeaderRow(IXLWorksheet ws, string[] requiredKeys)
    {
        var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 10);
        for (int r = 1; r <= lastRow; r++)
        {
            var cells = ws.Row(r).CellsUsed()
                          .Select(c => c.GetString().Trim().ToLower())
                          .ToHashSet();
            if (requiredKeys.All(k => cells.Any(c => c.Contains(k))))
                return r;
        }
        return 0;
    }

    /// <summary>Строит словарь: нормализованный заголовок → номер колонки.</summary>
    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = cell.GetString().Trim().ToLower();
            // Нормализуем распространённые варианты написания
            if (key.Contains("фио") || key.Contains("имя") || key.Contains("ф.и.о")) key = "фио";
            else if (key.Contains("логин") || key.Contains("username"))               key = "логин";
            else if (key.Contains("email") || key.Contains("почта"))                  key = "email";
            else if (key.Contains("роль") || key.Contains("role"))                    key = "роль";
            else if (key.Contains("блок") || key.Contains("block"))                   key = "блок";
            else if (key.Contains("комнат") || key.Contains("room"))                  key = "комната";

            if (!map.ContainsKey(key))
                map[key] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static string GetCell(IXLRow row, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : "";

    private static bool IsRowEmpty(IXLRow row)
        => !row.CellsUsed().Any() || row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString()));
}
