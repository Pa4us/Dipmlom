using SharedModel.DTOs;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace WebAPP.Services;

/// <summary>
/// Парсинг PDF-файлов для импорта данных.
/// Использует PdfPig для извлечения текста с учётом позиций слов.
///
/// Факультет в PDF хранится в виде аббревиатуры (ФАИС / ГЭФ / МТФ / МСФ / ЭФ).
/// При импорте аббревиатура разворачивается в полное название через <see cref="ExpandFaculty"/>.
/// </summary>
public static class PdfImportService
{
    // Блок вида "11-1", "91-2" — 2-3 цифры, дефис, 1-2 цифры.
    private const string BlockPattern = @"^\d{2,3}-\d{1,2}$";

    // Суффикс телефона после переноса: "240-90-80"
    private const string PhoneSuffixPattern = @"^\d{2,3}-\d{2}-\d{2}$";

    // ─── Обратный справочник: аббревиатура → полное название ─────────────────
    // Должен соответствовать FacultyAbbreviations в PdfExportService.
    private static readonly Dictionary<string, string> FacultyExpansions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ФАИС"] = "Факультет автоматизированных и информационных систем",
            ["ГЭФ"]  = "Гуманитарно-экономический факультет",
            ["МТФ"]  = "Механико-технологический факультет",
            ["МСФ"]  = "Машиностроительный факультет",
            ["ЭФ"]   = "Энергетический факультет",
        };

    private static string? ExpandFaculty(string? faculty)
    {
        if (string.IsNullOrWhiteSpace(faculty)) return null;
        return FacultyExpansions.TryGetValue(faculty.Trim(), out var full) ? full : faculty.Trim();
    }

    // ─── Основной метод ───────────────────────────────────────────────────────

    /// <summary>
    /// Извлекает список студентов из PDF-файла заселения.
    /// Формат: таблица с колонками ФИО | Телефон | Факультет | Группа | Блок.
    /// </summary>
    public static (List<CheckInItemRowDto> rows, string? error) ParseCheckInFile(Stream stream)
    {
        try
        {
            var rows   = new List<CheckInItemRowDto>();
            int rowNum = 1;

            using var document = PdfDocument.Open(stream);

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (!words.Any()) continue;

                var lines = GroupIntoLines(words, yTolerance: 5.0);
                CheckInItemRowDto? lastRow = null;

                foreach (var lineWords in lines)
                {
                    var sorted  = lineWords.OrderBy(w => w.BoundingBox.Left).ToList();
                    var columns = SplitByXGap(sorted, minGap: 14.0);

                    CheckInItemRowDto? row;
                    if (columns.Count >= 2)
                        row = TryParseColumns(columns, rowNum);
                    else
                    {
                        var lineText = string.Join(" ", sorted.Select(w => w.Text)).Trim();
                        row = TryParseTextLine(lineText, rowNum);
                    }

                    if (row != null)
                    {
                        rows.Add(row);
                        lastRow = row;
                        rowNum++;
                    }
                    else if (lastRow != null)
                    {
                        TryMergeContinuation(lastRow, sorted, columns);
                    }
                }
            }

            if (!rows.Any())
                return (rows,
                    "Не удалось распознать данные в PDF. " +
                    "Убедитесь, что файл содержит таблицу с колонками: ФИО, Телефон, Факультет, Группа, Блок.");

            return (rows, null);
        }
        catch (Exception ex)
        {
            return (new List<CheckInItemRowDto>(), $"Ошибка чтения PDF: {ex.Message}");
        }
    }

    // ─── Склейка перенесённых строк ──────────────────────────────────────────

    private static void TryMergeContinuation(
        CheckInItemRowDto lastRow,
        List<Word> sorted,
        List<string> columns)
    {
        var text = string.Join(" ", sorted.Select(w => w.Text)).Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // Суффикс телефона: "240-90-80"
        if (System.Text.RegularExpressions.Regex.IsMatch(text, PhoneSuffixPattern)
            && lastRow.PhoneNumber?.StartsWith("+375") == true
            && !System.Text.RegularExpressions.Regex.IsMatch(
                lastRow.PhoneNumber, @"\d{3}-\d{2}-\d{2}"))
        {
            lastRow.PhoneNumber = lastRow.PhoneNumber.TrimEnd() + " " + text;
            return;
        }

        // Продолжение факультета (только буквы/пробел/дефис)
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[А-ЯЁа-яёA-Za-z\s\-]+$")
            && text.Length > 2)
        {
            if (string.IsNullOrEmpty(lastRow.Faculty))
                lastRow.Faculty = ExpandFaculty(text);
            else
                lastRow.Faculty = lastRow.Faculty!.TrimEnd('-').TrimEnd() + "-" + text;
            return;
        }

        if (columns.Count >= 2)
        {
            foreach (var col in columns)
            {
                var c = col.Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(c, PhoneSuffixPattern)
                    && lastRow.PhoneNumber?.StartsWith("+375") == true
                    && !System.Text.RegularExpressions.Regex.IsMatch(
                        lastRow.PhoneNumber, @"\d{3}-\d{2}-\d{2}"))
                {
                    lastRow.PhoneNumber = lastRow.PhoneNumber.TrimEnd() + " " + c;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(c, @"^[А-ЯЁа-яёA-Za-z\s\-]+$")
                         && c.Length > 2 && string.IsNullOrEmpty(lastRow.Faculty))
                {
                    lastRow.Faculty = ExpandFaculty(c);
                }
            }
        }
    }

    // ─── Разбивка на строки по Y ──────────────────────────────────────────────

    private static List<List<Word>> GroupIntoLines(IEnumerable<Word> words, double yTolerance)
    {
        var sorted = words
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        var lines = new List<List<Word>>();
        if (!sorted.Any()) return lines;

        var currentLine = new List<Word> { sorted[0] };
        double currentY = sorted[0].BoundingBox.Bottom;

        for (int i = 1; i < sorted.Count; i++)
        {
            var w = sorted[i];
            if (Math.Abs(w.BoundingBox.Bottom - currentY) <= yTolerance)
                currentLine.Add(w);
            else
            {
                lines.Add(currentLine);
                currentLine = new List<Word> { w };
                currentY    = w.BoundingBox.Bottom;
            }
        }
        lines.Add(currentLine);
        return lines;
    }

    // ─── Разбивка на колонки по X-gap ────────────────────────────────────────

    private static List<string> SplitByXGap(List<Word> sorted, double minGap)
    {
        if (!sorted.Any()) return new();

        var groups     = new List<List<Word>>();
        var currentGrp = new List<Word> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            var gap = sorted[i].BoundingBox.Left - sorted[i - 1].BoundingBox.Right;
            if (gap >= minGap) { groups.Add(currentGrp); currentGrp = new List<Word>(); }
            currentGrp.Add(sorted[i]);
        }
        groups.Add(currentGrp);

        return groups.Select(g => string.Join(" ", g.Select(w => w.Text)).Trim()).ToList();
    }

    // ─── Разбор по готовым колонкам ───────────────────────────────────────────

    private static CheckInItemRowDto? TryParseColumns(List<string> cols, int rowNum)
    {
        var fio = cols[0].Trim();
        if (string.IsNullOrWhiteSpace(fio) || !fio.Any(char.IsLetter)) return null;
        if (IsHeaderValue(fio)) return null;

        string? phone       = cols.Count > 1 ? cols[1] : null;
        string? faculty     = cols.Count > 2 ? cols[2] : null;
        string? group       = cols.Count > 3 ? cols[3] : null;
        string? blockNumber = cols.Count > 4 ? cols[4] : null;

        // Совместимость со старым форматом (без телефона)
        if (!string.IsNullOrWhiteSpace(phone) &&
            System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), BlockPattern))
        {
            blockNumber = phone; phone = null; faculty = null; group = null;
        }

        if (string.IsNullOrWhiteSpace(blockNumber))
            blockNumber = cols.Skip(1)
                .FirstOrDefault(c => System.Text.RegularExpressions.Regex.IsMatch(c.Trim(), BlockPattern));

        if (string.IsNullOrWhiteSpace(blockNumber)) return null;

        return new CheckInItemRowDto
        {
            RowNumber   = rowNum,
            FullName    = fio,
            PhoneNumber = NullIfEmpty(phone),
            Faculty     = ExpandFaculty(faculty),
            Group       = NullIfEmpty(group),
            BlockNumber = blockNumber.Trim(),
        };
    }

    // ─── Разбор текстовой строки с разделителями ─────────────────────────────

    private static CheckInItemRowDto? TryParseTextLine(string line, int rowNum)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        string[] parts;
        if (line.Contains('\t'))      parts = line.Split('\t');
        else if (line.Contains(';'))  parts = line.Split(';');
        else if (line.Contains('|'))  parts = line.Split('|');
        else parts = System.Text.RegularExpressions.Regex.Split(line, @"\s{2,}");

        parts = parts.Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
        if (parts.Length < 2) return null;

        var fio = parts[0];
        if (!fio.Any(char.IsLetter) || IsHeaderValue(fio)) return null;

        string? phone       = parts.Length > 1 ? parts[1] : null;
        string? faculty     = parts.Length > 2 ? parts[2] : null;
        string? group       = parts.Length > 3 ? parts[3] : null;
        string? blockNumber = parts.Length > 4 ? parts[4] : null;

        if (!string.IsNullOrWhiteSpace(phone) &&
            System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), BlockPattern))
        {
            blockNumber = phone; phone = null; faculty = null; group = null;
        }

        if (string.IsNullOrWhiteSpace(blockNumber))
        {
            var m = System.Text.RegularExpressions.Regex.Match(line, @"\b\d{2,3}-\d{1,2}\b");
            if (m.Success) blockNumber = m.Value;
        }

        if (string.IsNullOrWhiteSpace(blockNumber)) return null;

        return new CheckInItemRowDto
        {
            RowNumber   = rowNum,
            FullName    = fio,
            PhoneNumber = NullIfEmpty(phone),
            Faculty     = ExpandFaculty(faculty),
            Group       = NullIfEmpty(group),
            BlockNumber = blockNumber.Trim(),
        };
    }

    // ─── Утилиты ─────────────────────────────────────────────────────────────

    private static readonly string[] _headerKeywords =
        ["фио", "ф.и.о", "фамилия", "факультет", "группа", "блок", "номер", "телефон", "тел", "#", "п/п", "n/n", "name"];

    private static bool IsHeaderValue(string text)
    {
        var lower = text.ToLower().Trim();
        return _headerKeywords.Any(h => lower == h || lower.StartsWith(h + " "));
    }

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
