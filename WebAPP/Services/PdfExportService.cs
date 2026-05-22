using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedModel.DTOs;

namespace WebAPP.Services;

public static class PdfExportService
{
    // ─── Общие цвета ─────────────────────────────────────────────────────────

    private static readonly string HeaderBg   = "#2c5f8a";
    private static readonly string HeaderFg   = "#ffffff";
    private static readonly string RowAlt     = "#f5f7fa";
    private static readonly string ScoreHigh  = "#d4edda";
    private static readonly string ScoreMid   = "#fff3cd";
    private static readonly string ScoreLow   = "#f8d7da";
    private static readonly string GoldRow    = "#fff8dc";
    private static readonly string TitleColor = "#1a3a5c";

    // ─── Проверки блоков ─────────────────────────────────────────────────────

    public static Stream ExportInspectionsPdf(List<InspectionDto> inspections,
        string? dateFrom, string? dateTo, int? floor, string? blockNumber)
    {
        var subtitle = BuildSubtitle(dateFrom, dateTo, floor, blockNumber);
        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => BuildHeader(c, $"Результаты проверок{subtitle}",
                    $"ГГТУ им. П.О. Сухого · Общежитие · {DateTime.Today:dd.MM.yyyy}"));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(60);   // Дата
                        cols.ConstantColumn(45);   // Этаж
                        cols.ConstantColumn(65);   // Блок
                        cols.RelativeColumn(2);    // Зона
                        cols.RelativeColumn(2);    // Инспектор
                        cols.ConstantColumn(55);   // Оценка
                        cols.RelativeColumn(3);    // Комментарий
                    });

                    // Шапка
                    var headers = new[] { "Дата", "Этаж", "Блок", "Зона", "Инспектор", "Оценка", "Комментарий" };
                    table.Header(h =>
                    {
                        foreach (var hdr in headers)
                        {
                            h.Cell().Background(HeaderBg).Padding(5).AlignCenter()
                                .Text(hdr).FontColor(HeaderFg).Bold().FontSize(9);
                        }
                    });

                    // Данные
                    for (int idx = 0; idx < inspections.Count; idx++)
                    {
                        var i   = inspections[idx];
                        var bg  = i.Score >= 8 ? ScoreHigh : i.Score >= 5 ? ScoreMid : ScoreLow;
                        var alt = idx % 2 == 1 ? RowAlt : "#ffffff";
                        var rowBg = i.Score < 8 ? bg : alt;

                        void Cell(Action<IContainer> content) =>
                            table.Cell().Background(rowBg).BorderBottom(0.3f).BorderColor("#dee2e6")
                                .Padding(4).Element(c => content(c));

                        Cell(c => c.AlignCenter().Text(i.InspectionDate.ToString("dd.MM.yyyy")));
                        Cell(c => c.AlignCenter().Text(i.Floor.ToString()));
                        Cell(c => c.AlignCenter().Text(i.BlockNumber ?? "—"));
                        Cell(c => c.Text(i.ZoneName ?? "—"));
                        Cell(c => c.Text(i.InspectorName ?? "—"));
                        Cell(c => c.AlignCenter().Text(i.Score.ToString())
                                    .Bold().FontColor(i.Score < 5 ? "#dc2626" : "#1a3a5c"));
                        Cell(c => c.Text(i.Comment ?? ""));
                    }
                });

                // Итоги
                if (inspections.Any())
                {
                    page.Content().PaddingTop(6).Row(row =>
                    {
                        row.AutoItem().Text(
                            $"Всего проверок: {inspections.Count}   " +
                            $"Средний балл: {inspections.Average(x => x.Score):F1}   " +
                            $"Неудовлетворительных (< 5): {inspections.Count(x => x.Score < 5)}"
                        ).FontSize(9).FontColor("#555555");
                    });
                }

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    // ─── Рейтинг студентов ───────────────────────────────────────────────────

    public static Stream ExportStudentRatingsPdf(List<StudentRatingDto> ratings)
    {
        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(c => BuildHeader(c, "Рейтинг студентов",
                    $"ГГТУ им. П.О. Сухого · Общежитие · {DateTime.Today:dd.MM.yyyy}"));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(40);   // Место
                        cols.RelativeColumn(4);    // ФИО
                        cols.ConstantColumn(70);   // Итого
                        cols.ConstantColumn(80);   // За мероприятия
                        cols.ConstantColumn(70);   // Штрафные
                    });

                    var headers = new[] { "#", "ФИО студента", "Итого баллов", "За мероприятия", "Штрафные" };
                    table.Header(h =>
                    {
                        foreach (var hdr in headers)
                        {
                            h.Cell().Background(HeaderBg).Padding(6).AlignCenter()
                                .Text(hdr).FontColor(HeaderFg).Bold().FontSize(10);
                        }
                    });

                    int place = 1;
                    foreach (var r in ratings)
                    {
                        var bg       = place <= 3 ? GoldRow : place % 2 == 0 ? RowAlt : "#ffffff";
                        var pts      = r.TotalPoints;
                        var placeNow = place; // capture for lambda

                        void Cell(Action<IContainer> content) =>
                            table.Cell().Background(bg).BorderBottom(0.3f).BorderColor("#dee2e6")
                                .Padding(5).Element(c => content(c));

                        Cell(c =>
                        {
                            var label = placeNow <= 3 ? $"★ {placeNow}" : placeNow.ToString();
                            var color = placeNow == 1 ? "#b8860b" : "#1a3a5c";
                            if (placeNow <= 3)
                                c.AlignCenter().Text(label).Bold().FontColor(color);
                            else
                                c.AlignCenter().Text(label).FontColor(color);
                        });
                        Cell(c => c.Text(r.FullName));
                        Cell(c => c.AlignCenter().Text($"{(pts >= 0 ? "+" : "")}{pts}")
                                    .Bold().FontColor(pts >= 0 ? "#16a34a" : "#dc2626"));
                        Cell(c => c.AlignCenter().Text($"+{r.EventPoints}").FontColor("#555555"));
                        Cell(c => c.AlignCenter().Text($"-{r.PenaltyPoints}").FontColor("#dc2626"));

                        place++;
                    }
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    // ─── Учётные данные после заселения ──────────────────────────────────────

    public static Stream ExportCheckInCredentialsPdf(List<CheckInRequestItemDto> items)
    {
        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(c => BuildHeader(c, "Учётные данные новых студентов",
                    $"ГГТУ им. П.О. Сухого · Общежитие · {DateTime.Today:dd.MM.yyyy}"));

                page.Content().Column(col =>
                {
                    col.Item().Text("Сохраните эти данные. После закрытия страницы пароли не отображаются.")
                        .FontSize(9).FontColor("#dc2626").Italic();
                    col.Item().PaddingBottom(8);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);   // ФИО
                            cols.ConstantColumn(90);  // Телефон
                            cols.RelativeColumn(2);   // Факультет/Группа
                            cols.ConstantColumn(70);  // Логин
                            cols.ConstantColumn(80);  // Пароль
                            cols.ConstantColumn(70);  // Блок
                        });

                        var headers = new[] { "ФИО", "Телефон", "Факультет / Группа", "Логин", "Пароль", "Блок" };
                        table.Header(h =>
                        {
                            foreach (var hdr in headers)
                                h.Cell().Background(HeaderBg).Padding(5).AlignCenter()
                                    .Text(hdr).FontColor(HeaderFg).Bold().FontSize(9);
                        });

                        for (int idx = 0; idx < items.Count; idx++)
                        {
                            var item = items[idx];
                            var bg   = idx % 2 == 0 ? "#ffffff" : RowAlt;

                            void Cell(Action<IContainer> c) =>
                                table.Cell().Background(bg).BorderBottom(0.3f).BorderColor("#dee2e6")
                                    .Padding(4).Element(c);

                            Cell(c => c.Text(item.FullName));
                            Cell(c => c.AlignCenter().Text(item.PhoneNumber ?? "—").FontSize(9).FontColor("#555555"));
                            Cell(c => c.Text($"{item.Faculty ?? "—"} / {item.Group ?? "—"}").FontSize(9));
                            Cell(c => c.AlignCenter().Text(item.Username ?? "—").Bold().FontColor(TitleColor));
                            Cell(c => c.AlignCenter().Text(item.GeneratedPassword ?? "—")
                                        .Bold().FontColor("#dc2626").FontFamily("Courier New"));
                            Cell(c => c.AlignCenter().Text(item.BlockNumber));
                        }
                    });
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    // ─── Список студентов для заселения (тестовый / формируемый менеджером) ──

    // ─── Справочник аббревиатур факультетов ──────────────────────────────────
    // Ключ — любое вхождение (регистронезависимо), значение — аббревиатура.
    // При экспорте полное название заменяется аббревиатурой, чтобы текст
    // в ячейке таблицы гарантированно не переносился на новую строку.
    private static readonly (string Key, string Abbr)[] FacultyAbbreviations =
    [
        ("факультет автоматизированных",       "ФАИС"),
        ("гуманитарно-экономич",               "ГЭФ"),
        ("механико-технологич",                "МТФ"),
        ("машиностроительн",                   "МСФ"),
        ("энергетическ",                       "ЭФ"),
    ];

    private static string AbbreviateFaculty(string? faculty)
    {
        if (string.IsNullOrWhiteSpace(faculty)) return "";
        foreach (var (key, abbr) in FacultyAbbreviations)
            if (faculty.Contains(key, StringComparison.OrdinalIgnoreCase))
                return abbr;
        return faculty; // неизвестный факультет — оставляем как есть
    }

    /// <summary>
    /// Генерирует PDF со списком студентов для заселения.
    /// Факультет выводится аббревиатурой (ФАИС / ГЭФ / МТФ / МСФ / ЭФ),
    /// что освобождает ширину и полностью устраняет перенос строк в ячейках.
    /// При импорте PdfImportService разворачивает аббревиатуру обратно.
    /// </summary>
    public static Stream ExportCheckInListPdf(List<CheckInItemRowDto> rows)
    {
        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => BuildHeader(c,
                    "Список студентов для заселения",
                    $"ГГТУ им. П.О. Сухого · Общежитие · {DateTime.Today:dd.MM.yyyy}"));

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        // Факультет теперь занимает 2 части вместо 7:
                        // аббревиатура (≤4 симв.) не переносится никогда.
                        // Телефон получает больше места и тоже не переносится.
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);    // ФИО      ≈ 261 pt
                            cols.RelativeColumn(4);    // Телефон  ≈ 209 pt  ("+375 (XX) NNN-NN-NN" = ~95 pt)
                            cols.RelativeColumn(2);    // Факультет ≈ 104 pt ("ФАИС" = ~20 pt)
                            cols.RelativeColumn(2);    // Группа   ≈ 104 pt
                            cols.RelativeColumn(1.5f); // Блок     ≈  78 pt
                        });

                        var headers = new[] { "ФИО", "Телефон", "Факультет", "Группа", "Блок" };
                        table.Header(h =>
                        {
                            foreach (var hdr in headers)
                                h.Cell().Background(HeaderBg).Padding(5).AlignCenter()
                                    .Text(hdr).FontColor(HeaderFg).Bold().FontSize(9);
                        });

                        for (int idx = 0; idx < rows.Count; idx++)
                        {
                            var r  = rows[idx];
                            var bg = idx % 2 == 0 ? "#ffffff" : RowAlt;

                            void Cell(string txt) =>
                                table.Cell().Background(bg)
                                    .BorderBottom(0.3f).BorderColor("#dee2e6")
                                    .Padding(4).Text(txt).FontSize(9);

                            Cell(r.FullName);
                            Cell(r.PhoneNumber ?? "");
                            Cell(AbbreviateFaculty(r.Faculty));
                            Cell(r.Group    ?? "");
                            Cell(r.BlockNumber);
                        }
                    });

                    col.Item().PaddingTop(12)
                        .Text($"Итого студентов: {rows.Count}")
                        .FontSize(9).FontColor("#555555");

                    col.Item().PaddingTop(4)
                        .Text("Документ сформирован системой. Для загрузки используйте раздел «Заселение → Загрузить PDF».")
                        .FontSize(8).FontColor("#888888").Italic();
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    // ─── Проживание (Заведующая) ─────────────────────────────────────────────

    public static Stream ExportResidencesPdf(List<ResidenceDto> residences)
    {
        var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => BuildHeader(c, "Текущие жильцы общежития",
                    $"ГГТУ им. П.О. Сухого · Общежитие · {DateTime.Today:dd.MM.yyyy}"));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(25);   // №
                        cols.RelativeColumn(3);    // ФИО
                        cols.RelativeColumn(1.5f); // Логин
                        cols.ConstantColumn(40);   // Этаж
                        cols.ConstantColumn(55);   // Блок
                        cols.ConstantColumn(55);   // Комната
                        cols.ConstantColumn(75);   // Дата заселения
                        cols.ConstantColumn(75);   // Дата выселения
                    });

                    var headers = new[] { "№", "ФИО", "Логин", "Этаж", "Блок", "Комната", "Заселён с", "Выселен" };
                    table.Header(h =>
                    {
                        foreach (var hdr in headers)
                            h.Cell().Background(HeaderBg).Padding(5).AlignCenter()
                                .Text(hdr).FontColor(HeaderFg).Bold().FontSize(9);
                    });

                    for (int idx = 0; idx < residences.Count; idx++)
                    {
                        var r  = residences[idx];
                        var bg = idx % 2 == 0 ? "#ffffff" : RowAlt;

                        void Cell(Action<IContainer> content) =>
                            table.Cell().Background(bg).BorderBottom(0.3f).BorderColor("#dee2e6")
                                .Padding(4).Element(c => content(c));

                        Cell(c => c.AlignCenter().Text((idx + 1).ToString()).FontColor("#888888"));
                        Cell(c => c.Text(r.UserFullName ?? "—"));
                        Cell(c => c.Text(r.Username ?? "—").FontColor("#555555"));
                        Cell(c => c.AlignCenter().Text(r.Floor.ToString()));
                        Cell(c => c.AlignCenter().Text(r.BlockNumber ?? "—").Bold());
                        Cell(c => c.AlignCenter().Text(r.RoomNumber ?? "—"));
                        Cell(c => c.AlignCenter().Text(r.MoveInDate.ToString("dd.MM.yyyy")));
                        Cell(c => c.AlignCenter().Text(
                            r.MoveOutDate.HasValue ? r.MoveOutDate.Value.ToString("dd.MM.yyyy") : "Проживает")
                            .FontColor(r.MoveOutDate.HasValue ? "#dc2626" : "#16a34a"));
                    }
                });

                if (residences.Any())
                {
                    page.Content().PaddingTop(6)
                        .Text($"Всего жильцов: {residences.Count}")
                        .FontSize(9).FontColor("#555555");
                }

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf(ms);

        ms.Position = 0;
        return ms;
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    private static void BuildHeader(IContainer c, string title, string subtitle)
    {
        c.Column(col =>
        {
            col.Item().Text(title).FontSize(16).Bold().FontColor(TitleColor);
            col.Item().Text(subtitle).FontSize(9).FontColor("#666666");
            col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#2c5f8a");
            col.Item().PaddingBottom(4);
        });
    }

    private static void BuildFooter(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().AlignLeft()
                .Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .FontSize(8).FontColor("#aaaaaa");
            row.RelativeItem().AlignRight()
                .Text(txt =>
                {
                    txt.Span("Стр. ").FontSize(8).FontColor("#aaaaaa");
                    txt.CurrentPageNumber().FontSize(8).FontColor("#aaaaaa");
                    txt.Span(" / ").FontSize(8).FontColor("#aaaaaa");
                    txt.TotalPages().FontSize(8).FontColor("#aaaaaa");
                });
        });
    }

    private static string BuildSubtitle(string? dateFrom, string? dateTo, int? floor, string? blockNumber)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(dateFrom) || !string.IsNullOrEmpty(dateTo))
            parts.Add($"{dateFrom ?? "…"} – {dateTo ?? "…"}");
        if (floor.HasValue) parts.Add($"{floor} этаж");
        if (!string.IsNullOrEmpty(blockNumber)) parts.Add($"блок {blockNumber}");
        return parts.Any() ? $" ({string.Join(", ", parts)})" : "";
    }
}
