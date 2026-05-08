using ClosedXML.Excel;
using SharedModel.DTOs;
using WebAPP.Models.ViewModels;

namespace WebAPP.Services;

/// <summary>
/// Генерирует Excel-файлы для скачивания.
/// Возвращает поток, который контроллер отдаёт как FileStreamResult.
/// </summary>
public static class ExcelExportService
{
    // ─── Стили ───────────────────────────────────────────────────────────────

    private static void StyleHeader(IXLRow row, int colCount)
    {
        var range = row.Worksheet.Range(row.RowNumber(), 1, row.RowNumber(), colCount);
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c5f8a");
        range.Style.Font.FontColor       = XLColor.White;
        range.Style.Font.Bold            = true;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void AutoFitAndFreeze(IXLWorksheet ws, int headerRow = 1)
    {
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);
    }

    private static void SetTitle(IXLWorksheet ws, string title, int colSpan)
    {
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, colSpan).Merge();
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Font.FontSize = 13;
        ws.Row(1).Height = 22;
    }

    // ─── Воспитатель: проверки ────────────────────────────────────────────────

    public static Stream ExportInspections(List<InspectionDto> inspections,
        string? dateFrom, string? dateTo, int? floor, string? blockNumber)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Проверки");

        const int cols = 7;

        // Заголовок
        var subtitle = BuildInspectionSubtitle(dateFrom, dateTo, floor, blockNumber);
        SetTitle(ws, $"Результаты проверок{subtitle}", cols);

        // Шапка таблицы
        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "Дата";
        hdr.Cell(2).Value = "Этаж";
        hdr.Cell(3).Value = "Блок";
        hdr.Cell(4).Value = "Зона";
        hdr.Cell(5).Value = "Инспектор";
        hdr.Cell(6).Value = "Оценка";
        hdr.Cell(7).Value = "Комментарий";
        StyleHeader(hdr, cols);

        // Данные
        int row = 3;
        foreach (var i in inspections)
        {
            ws.Cell(row, 1).Value = i.InspectionDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = i.Floor;
            ws.Cell(row, 3).Value = i.BlockNumber;
            ws.Cell(row, 4).Value = i.ZoneName;
            ws.Cell(row, 5).Value = i.InspectorName;
            ws.Cell(row, 6).Value = i.Score;
            ws.Cell(row, 7).Value = i.Comment ?? "";

            // Цвет строки по оценке
            var fill = i.Score >= 8 ? XLColor.FromHtml("#d4edda")
                     : i.Score >= 5 ? XLColor.FromHtml("#fff3cd")
                                    : XLColor.FromHtml("#f8d7da");
            ws.Row(row).Style.Fill.BackgroundColor = fill;

            // Жирный шрифт для низкой оценки
            if (i.Score < 5)
                ws.Row(row).Style.Font.Bold = true;

            row++;
        }

        // Итоги
        if (inspections.Any())
        {
            row++;
            ws.Cell(row, 1).Value = "Итого проверок:";
            ws.Cell(row, 2).Value = inspections.Count;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = "Средний балл:";
            ws.Cell(row, 2).Value = Math.Round(inspections.Average(i => i.Score), 2);
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = "Неудовлетворительных (< 5):";
            ws.Cell(row, 2).Value = inspections.Count(i => i.Score < 5);
            ws.Cell(row, 1).Style.Font.Bold = true;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        ws.Column(7).Width = 40; // Комментарий шире

        return ToStream(wb);
    }

    private static string BuildInspectionSubtitle(string? dateFrom, string? dateTo,
        int? floor, string? blockNumber)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(dateFrom) || !string.IsNullOrEmpty(dateTo))
            parts.Add($"{dateFrom ?? "…"} – {dateTo ?? "…"}");
        if (floor.HasValue) parts.Add($"{floor} этаж");
        if (!string.IsNullOrEmpty(blockNumber)) parts.Add($"блок {blockNumber}");
        return parts.Any() ? $" ({string.Join(", ", parts)})" : "";
    }

    // ─── Воспитатель: рейтинг студентов ──────────────────────────────────────

    public static Stream ExportStudentRatings(List<StudentRatingDto> ratings)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Рейтинг студентов");

        const int cols = 5;
        SetTitle(ws, $"Рейтинг студентов — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "Место";
        hdr.Cell(2).Value = "ФИО";
        hdr.Cell(3).Value = "Итого баллов";
        hdr.Cell(4).Value = "За мероприятия";
        hdr.Cell(5).Value = "Штрафные";
        StyleHeader(hdr, cols);

        int row = 3;
        int place = 1;
        foreach (var r in ratings)
        {
            ws.Cell(row, 1).Value = place;
            ws.Cell(row, 2).Value = r.FullName;
            ws.Cell(row, 3).Value = r.TotalPoints;
            ws.Cell(row, 4).Value = r.EventPoints;
            ws.Cell(row, 5).Value = r.PenaltyPoints;

            // Топ-3 золотом
            if (place <= 3)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff8dc");

            place++;
            row++;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        return ToStream(wb);
    }

    // ─── Воспитатель: список студентов ────────────────────────────────────────

    public static Stream ExportStudentsList(List<StudentCardDto> students)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Студенты");

        const int cols = 7;
        SetTitle(ws, $"Список студентов — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ФИО";
        hdr.Cell(2).Value = "Логин";
        hdr.Cell(3).Value = "Email";
        hdr.Cell(4).Value = "Этаж";
        hdr.Cell(5).Value = "Блок";
        hdr.Cell(6).Value = "Комната";
        hdr.Cell(7).Value = "Баллы";
        StyleHeader(hdr, cols);

        int row = 3;
        foreach (var s in students)
        {
            ws.Cell(row, 1).Value = s.User.FullName;
            ws.Cell(row, 2).Value = s.User.Username;
            ws.Cell(row, 3).Value = s.User.Email;
            ws.Cell(row, 4).Value = s.Residence?.Floor.ToString() ?? "—";
            ws.Cell(row, 5).Value = s.Residence?.BlockNumber ?? "—";
            ws.Cell(row, 6).Value = s.Residence?.RoomNumber?.ToString() ?? "—";
            ws.Cell(row, 7).Value = s.Rating?.TotalPoints ?? 0;

            // Чередование фона
            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f5f5");

            row++;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        return ToStream(wb);
    }

    // ─── Заведующая: аккаунты ─────────────────────────────────────────────────

    public static Stream ExportUsers(List<UserDto> users)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Аккаунты");

        const int cols = 6;
        SetTitle(ws, $"Аккаунты пользователей — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ID";
        hdr.Cell(2).Value = "ФИО";
        hdr.Cell(3).Value = "Логин";
        hdr.Cell(4).Value = "Email";
        hdr.Cell(5).Value = "Роль";
        hdr.Cell(6).Value = "Активен";
        StyleHeader(hdr, cols);

        int row = 3;
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Id;
            ws.Cell(row, 2).Value = u.FullName;
            ws.Cell(row, 3).Value = u.Username;
            ws.Cell(row, 4).Value = u.Email;
            ws.Cell(row, 5).Value = u.RoleName;
            ws.Cell(row, 6).Value = u.IsActive ? "Да" : "Нет";

            if (!u.IsActive)
                ws.Row(row).Style.Font.FontColor = XLColor.Gray;

            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f5f5");

            row++;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        return ToStream(wb);
    }

    // ─── Заведующая: проживание ───────────────────────────────────────────────

    public static Stream ExportResidences(List<ResidenceDto> residences)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Проживание");

        const int cols = 7;
        SetTitle(ws, $"Текущие жильцы — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ФИО";
        hdr.Cell(2).Value = "Логин";
        hdr.Cell(3).Value = "Этаж";
        hdr.Cell(4).Value = "Блок";
        hdr.Cell(5).Value = "Комната";
        hdr.Cell(6).Value = "Дата заселения";
        hdr.Cell(7).Value = "Дата выселения";
        StyleHeader(hdr, cols);

        int row = 3;
        foreach (var r in residences)
        {
            ws.Cell(row, 1).Value = r.UserFullName ?? "—";
            ws.Cell(row, 2).Value = r.Username ?? "—";
            ws.Cell(row, 3).Value = r.Floor;
            ws.Cell(row, 4).Value = r.BlockNumber ?? "—";
            ws.Cell(row, 5).Value = r.RoomNumber?.ToString() ?? "—";
            ws.Cell(row, 6).Value = r.MoveInDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 7).Value = r.MoveOutDate.HasValue ? r.MoveOutDate.Value.ToString("dd.MM.yyyy") : "Проживает";

            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f5f5");

            row++;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        return ToStream(wb);
    }

    // ─── Заведующая: заявки на ремонт ────────────────────────────────────────

    public static Stream ExportRepairRequests(List<RepairRequestDto> requests, string? statusFilter)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Заявки на ремонт");

        const int cols = 8;
        var subtitle = string.IsNullOrEmpty(statusFilter) ? "" : $" (статус: {statusFilter})";
        SetTitle(ws, $"Заявки на ремонт{subtitle} — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ID";
        hdr.Cell(2).Value = "Описание";
        hdr.Cell(3).Value = "Блок";
        hdr.Cell(4).Value = "Комната";
        hdr.Cell(5).Value = "Подано";
        hdr.Cell(6).Value = "Статус";
        hdr.Cell(7).Value = "Кто подал";
        hdr.Cell(8).Value = "Назначен";
        StyleHeader(hdr, cols);

        int row = 3;
        foreach (var r in requests)
        {
            ws.Cell(row, 1).Value = r.Id;
            ws.Cell(row, 2).Value = r.Description;
            ws.Cell(row, 3).Value = r.BlockNumber ?? "—";
            ws.Cell(row, 4).Value = r.RoomNumber?.ToString() ?? "—";
            ws.Cell(row, 5).Value = r.CreatedAt.ToString("dd.MM.yyyy");
            ws.Cell(row, 6).Value = r.Status;
            ws.Cell(row, 7).Value = r.RequestedByName ?? "—";
            ws.Cell(row, 8).Value = r.AssignedToName ?? "—";

            // Цвет по статусу
            var fill = r.Status switch
            {
                "Completed"  => XLColor.FromHtml("#d4edda"),
                "InProgress" => XLColor.FromHtml("#cce5ff"),
                "Rejected"   => XLColor.FromHtml("#f8d7da"),
                _            => row % 2 == 0 ? XLColor.FromHtml("#f5f5f5") : XLColor.White,
            };
            ws.Row(row).Style.Fill.BackgroundColor = fill;

            row++;
        }

        AutoFitAndFreeze(ws, headerRow: 2);
        ws.Column(2).Width = 45;
        return ToStream(wb);
    }

    // ─── Шаблон для импорта аккаунтов ────────────────────────────────────────

    public static Stream ExportImportUsersTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Аккаунты");

        ws.Cell(1, 1).Value = "Шаблон импорта аккаунтов — заполните начиная со строки 3";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Row(1).Style.Font.Italic = true;
        ws.Row(1).Style.Font.FontColor = XLColor.Gray;

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ФИО";
        hdr.Cell(2).Value = "Логин";
        hdr.Cell(3).Value = "Email";
        hdr.Cell(4).Value = "Роль";
        StyleHeader(hdr, 4);

        // Пример
        ws.Cell(3, 1).Value = "Иванов Иван Иванович";
        ws.Cell(3, 2).Value = "ivanov";
        ws.Cell(3, 3).Value = "ivanov@ggtu.by";
        ws.Cell(3, 4).Value = "Student";
        ws.Row(3).Style.Font.FontColor = XLColor.LightGray;

        // Подсказка по ролям
        ws.Cell(5, 1).Value = "Доступные роли: Student, Inspector, Educator, Mechanic, Manager";
        ws.Range(5, 1, 5, 4).Merge();
        ws.Row(5).Style.Font.Italic = true;
        ws.Row(5).Style.Font.FontColor = XLColor.Gray;

        ws.Columns().AdjustToContents();
        return ToStream(wb);
    }

    // ─── Шаблон для импорта заселения ────────────────────────────────────────

    public static Stream ExportImportResidencesTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Заселение");

        ws.Cell(1, 1).Value = "Шаблон импорта заселения — заполните начиная со строки 3";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Row(1).Style.Font.Italic = true;
        ws.Row(1).Style.Font.FontColor = XLColor.Gray;

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "Логин";
        hdr.Cell(2).Value = "ФИО";
        hdr.Cell(3).Value = "Блок";
        hdr.Cell(4).Value = "Комната";
        StyleHeader(hdr, 4);

        // Пример
        ws.Cell(3, 1).Value = "ivanov";
        ws.Cell(3, 2).Value = "Иванов Иван Иванович";
        ws.Cell(3, 3).Value = "44-1";
        ws.Cell(3, 4).Value = "";
        ws.Row(3).Style.Font.FontColor = XLColor.LightGray;

        ws.Cell(5, 1).Value = "Колонка «Комната» необязательна — если не указана, студент будет заселён в первую свободную комнату блока";
        ws.Range(5, 1, 5, 4).Merge();
        ws.Row(5).Style.Font.Italic = true;
        ws.Row(5).Style.Font.FontColor = XLColor.Gray;

        ws.Columns().AdjustToContents();
        return ToStream(wb);
    }

    // ─── Отчёт с паролями после импорта аккаунтов ────────────────────────────

    public static Stream ExportImportedPasswords(List<ImportUserRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Пароли");

        const int cols = 4;
        SetTitle(ws, $"Созданные аккаунты — {DateTime.Today:dd.MM.yyyy}", cols);

        var hdr = ws.Row(2);
        hdr.Cell(1).Value = "ФИО";
        hdr.Cell(2).Value = "Логин";
        hdr.Cell(3).Value = "Email";
        hdr.Cell(4).Value = "Пароль";
        StyleHeader(hdr, cols);

        int row = 3;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.FullName;
            ws.Cell(row, 2).Value = r.Username;
            ws.Cell(row, 3).Value = r.Email;
            ws.Cell(row, 4).Value = r.GeneratedPassword;

            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f5f5");
            row++;
        }

        // Колонку с паролем делаем чуть шире
        ws.Columns().AdjustToContents();
        ws.Column(4).Width = Math.Max(ws.Column(4).Width, 14);
        ws.SheetView.FreezeRows(2);
        return ToStream(wb);
    }

    // ─── Хелпер: сохраняем в MemoryStream ────────────────────────────────────

    private static Stream ToStream(XLWorkbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
