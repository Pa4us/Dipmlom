using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Globalization;
using System.Text.Json;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers;

[Authorize(Roles = "Educator")]
public class EducatorController : Controller
{
    private readonly ApiClient _api;
    private readonly InspectionScheduleService _schedule;

    public EducatorController(ApiClient api, InspectionScheduleService schedule)
    {
        _api = api;
        _schedule = schedule;
    }

    // ─── Дашборд ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Дашборд";
        var statsTask = _api.GetAsync<DormitoryStatisticsDto>("api/statistics/dormitory");
        var inspTask  = _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        await Task.WhenAll(statsTask, inspTask);

        var stats = statsTask.Result?.Data ?? new();
        var allScores = stats.Floors.SelectMany(f => f.BlockScores).OrderByDescending(b => b.Score).ToList();

        var vm = new EducatorDashboardViewModel
        {
            Stats = stats,
            RecentInspections = inspTask.Result?.Data?
                .OrderByDescending(i => i.InspectionDate).Take(8).ToList() ?? new(),
            BestBlocks  = allScores.Take(3).ToList(),
            WorstBlocks = allScores.TakeLast(3).ToList(),
        };
        return View(vm);
    }

    // ─── Пересчёт статистики ─────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> RecalculateStats()
    {
        await _api.PostAsync<object>("api/statistics/recalculate-all", new { });
        TempData["Success"] = "Статистика пересчитана";
        return RedirectToAction("Dashboard");
    }

    // ─── Проверки ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Inspections(string? dateFrom, string? dateTo, int? blockId, int? floor)
    {
        ViewData["Title"] = "Результаты проверок";

        // Блоки загружаем отдельно — нужны для фильтра
        var blocksResp = await _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var blocks = blocksResp?.Data?.ToList() ?? new();

        var inspResp = await _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        var list = inspResp?.Data?.ToList() ?? new();

        if (!string.IsNullOrEmpty(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
            list = list.Where(i => i.InspectionDate >= from).ToList();
        if (!string.IsNullOrEmpty(dateTo) && DateOnly.TryParse(dateTo, out var to))
            list = list.Where(i => i.InspectionDate <= to).ToList();

        if (floor.HasValue)
        {
            var blockIdsOnFloor = blocks.Where(b => b.Floor == floor.Value).Select(b => b.Id).ToHashSet();
            list = list.Where(i => blockIdsOnFloor.Contains(i.BlockId)).ToList();
        }

        if (blockId.HasValue)
            list = list.Where(i => i.BlockId == blockId.Value).ToList();

        var vm = new InspectionsFilterViewModel
        {
            Inspections     = list.OrderByDescending(i => i.InspectionDate).ToList(),
            Blocks          = blocks,
            DateFrom        = dateFrom,
            DateTo          = dateTo,
            SelectedBlockId = blockId,
            SelectedFloor   = floor,
        };
        return View(vm);
    }

    // ─── Баллы ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Points()
    {
        ViewData["Title"] = "Баллы студентов";

        // Студентов загружаем первыми — ключевые данные страницы
        var studentsResp = await _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Student");
        var students = studentsResp?.Data?.ToList() ?? new();

        var ratingsTask = _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        var eventsTask  = _api.GetAsync<IEnumerable<EventDto>>("api/events");
        await Task.WhenAll(ratingsTask, eventsTask);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var pastEvents = eventsTask.Result?.Data?
            .Where(e => e.EventDate < today)
            .OrderByDescending(e => e.EventDate)
            .ToList() ?? new();

        var participantMap = new Dictionary<int, List<UserDto>>();
        foreach (var ev in pastEvents)
        {
            var pResp = await _api.GetAsync<IEnumerable<UserDto>>($"api/events/{ev.Id}/participants");
            participantMap[ev.Id] = pResp?.Data?.ToList() ?? new();
        }

        var vm = new PointsViewModel
        {
            Ratings           = ratingsTask.Result?.Data?.OrderByDescending(r => r.TotalPoints).ToList() ?? new(),
            Students          = students,
            PastEvents        = pastEvents,
            EventParticipants = participantMap,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AwardPoints(AwardPointsDto dto)
    {
        var result = await _api.PostAsync<StudentPointDto>("api/studentpoints/award", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Баллы успешно начислены" : result?.Message ?? "Ошибка";
        return RedirectToAction("Points");
    }

    [HttpPost]
    public async Task<IActionResult> DeductPoints(DeductPointsDto dto)
    {
        var result = await _api.PostAsync<StudentPointDto>("api/studentpoints/deduct", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Баллы взысканы" : result?.Message ?? "Ошибка";
        return RedirectToAction("Points");
    }

    [HttpPost]
    public async Task<IActionResult> AwardAllEventParticipants(int eventId, int points, string reason)
    {
        var participantsResp = await _api.GetAsync<IEnumerable<UserDto>>($"api/events/{eventId}/participants");
        var participants = participantsResp?.Data?.ToList() ?? new();

        if (!participants.Any())
        {
            TempData["Error"] = "У мероприятия нет участников";
            return RedirectToAction("Points");
        }

        int success = 0;
        foreach (var p in participants)
        {
            var dto = new AwardPointsDto
            {
                UserId     = p.Id,
                Points     = points,
                Reason     = reason,
                SourceType = "Event",
                SourceId   = eventId,
            };
            var result = await _api.PostAsync<StudentPointDto>("api/studentpoints/award", dto);
            if (result?.Success == true) success++;
        }

        TempData["Success"] = $"Баллы начислены {success} из {participants.Count} участников";
        return RedirectToAction("Points");
    }

    // ─── Мероприятия ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Events()
    {
        ViewData["Title"] = "Мероприятия";

        // Студентов загружаем первыми
        var studentsResp = await _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Student");
        var students = studentsResp?.Data?.ToList() ?? new();

        var eventsResp = await _api.GetAsync<IEnumerable<EventDto>>("api/events");
        var events = eventsResp?.Data?.OrderBy(e => e.EventDate).ToList() ?? new();

        var participantMap = new Dictionary<int, List<UserDto>>();
        foreach (var ev in events)
        {
            var pResp = await _api.GetAsync<IEnumerable<UserDto>>($"api/events/{ev.Id}/participants");
            participantMap[ev.Id] = pResp?.Data?.ToList() ?? new();
        }

        var vm = new EventsViewModel
        {
            Events            = events,
            Students          = students,
            EventParticipants = participantMap,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        dto.OrganizerId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var result = await _api.PostAsync<EventDto>("api/events", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Мероприятие создано" : result?.Message ?? "Ошибка";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        await _api.DeleteAsync<bool>($"api/events/{id}");
        TempData["Success"] = "Мероприятие удалено";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> RegisterParticipant(int eventId, int studentId)
    {
        var result = await _api.PostAsync<EventDto>($"api/events/{eventId}/register/{studentId}", new { });
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Студент зарегистрирован" : result?.Message ?? "Ошибка";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> UnregisterParticipant(int eventId, int studentId)
    {
        var result = await _api.DeleteAsync<bool>($"api/events/{eventId}/unregister/{studentId}");
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Студент удалён из списка участников" : result?.Message ?? "Ошибка";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> AwardEventPoints(int eventId, int studentId, int points, string reason)
    {
        var dto = new AwardPointsDto
        {
            UserId     = studentId,
            Points     = points,
            Reason     = reason,
            SourceType = "Event",
            SourceId   = eventId,
        };
        var result = await _api.PostAsync<StudentPointDto>("api/studentpoints/award", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Баллы за мероприятие начислены" : result?.Message ?? "Ошибка";
        return RedirectToAction("Events");
    }

    // ─── Список студентов ─────────────────────────────────────────────────────

    public async Task<IActionResult> Students(string? search)
    {
        ViewData["Title"] = "Студенты";

        var studentsTask  = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Student");
        var residencesTask = _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        var ratingsTask   = _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        await Task.WhenAll(studentsTask, residencesTask, ratingsTask);

        var students  = studentsTask.Result?.Data?.ToList() ?? new();
        // Только актуальные (текущие) записи о проживании
        var residenceMap = residencesTask.Result?.Data?
            .Where(r => r.IsCurrent)
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<int, ResidenceDto>();
        var ratingMap = ratingsTask.Result?.Data?
            .ToDictionary(r => r.UserId)
            ?? new Dictionary<int, StudentRatingDto>();

        // Серверная фильтрация по поисковой строке (ФИО / логин / email)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            students = students
                .Where(s => s.FullName.ToLower().Contains(q)
                         || s.Username.ToLower().Contains(q)
                         || s.Email.ToLower().Contains(q))
                .ToList();
        }

        var cards = students.Select(s => new StudentCardDto
        {
            User      = s,
            Residence = residenceMap.GetValueOrDefault(s.Id),
            Rating    = ratingMap.GetValueOrDefault(s.Id),
        }).OrderBy(c => c.User.FullName).ToList();

        return View(new StudentsListViewModel { Students = cards, Search = search });
    }

    // ─── Импорт заселения из Excel ───────────────────────────────────────────

    [HttpGet]
    public IActionResult ImportResidences()
    {
        ViewData["Title"] = "Импорт заселения";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ImportResidences(IFormFile? file)
    {
        ViewData["Title"] = "Импорт заселения";

        if (file == null || file.Length == 0)
        { ModelState.AddModelError("", "Выберите файл Excel (.xlsx)"); return View(); }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        { ModelState.AddModelError("", "Поддерживается только формат .xlsx"); return View(); }

        using var stream = file.OpenReadStream();
        var (rows, parseError) = ExcelImportService.ParseResidencesFile(stream);
        if (parseError != null)
        { ModelState.AddModelError("", parseError); return View(); }

        var resp = await _api.PostAsync<ImportResidencesResultDto>(
            "api/residences/import",
            new ImportResidencesRequestDto { Rows = rows, DryRun = true });

        if (resp?.Success != true || resp.Data == null)
        { ModelState.AddModelError("", resp?.Message ?? "Ошибка при проверке файла"); return View(); }

        var vm = new ImportResidencesPreviewViewModel
        {
            Result        = resp.Data,
            ValidRowsJson = JsonSerializer.Serialize(resp.Data.ValidRows),
        };
        return View("ImportResidencesPreview", vm);
    }

    [HttpPost]
    public async Task<IActionResult> ImportResidencesConfirm(string validRowsJson)
    {
        List<ImportResidenceRowDto>? rows;
        try { rows = JsonSerializer.Deserialize<List<ImportResidenceRowDto>>(validRowsJson); }
        catch { TempData["Error"] = "Не удалось обработать данные. Попробуйте снова."; return RedirectToAction("ImportResidences"); }

        if (rows == null || rows.Count == 0)
        { TempData["Error"] = "Нет строк для импорта."; return RedirectToAction("ImportResidences"); }

        var resp = await _api.PostAsync<ImportResidencesResultDto>(
            "api/residences/import",
            new ImportResidencesRequestDto { Rows = rows, DryRun = false });

        if (resp?.Success != true || resp.Data == null)
        { TempData["Error"] = resp?.Message ?? "Ошибка при заселении"; return RedirectToAction("ImportResidences"); }

        return View("ImportResidencesResult", new ImportResidencesResultViewModel { Result = resp.Data });
    }

    public IActionResult ImportResidencesTemplate()
    {
        var stream = ExcelExportService.ExportImportResidencesTemplate();
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Шаблон_заселения.xlsx");
    }

    // ─── Экспорт в Excel ─────────────────────────────────────────────────────

    public async Task<IActionResult> ExportInspections(
        string? dateFrom, string? dateTo, int? blockId, int? floor)
    {
        var blocksResp = await _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var blocks     = blocksResp?.Data?.ToList() ?? new();

        var inspResp = await _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        var list     = inspResp?.Data?.ToList() ?? new();

        if (!string.IsNullOrEmpty(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
            list = list.Where(i => i.InspectionDate >= from).ToList();
        if (!string.IsNullOrEmpty(dateTo) && DateOnly.TryParse(dateTo, out var to))
            list = list.Where(i => i.InspectionDate <= to).ToList();
        if (floor.HasValue)
        {
            var ids = blocks.Where(b => b.Floor == floor.Value).Select(b => b.Id).ToHashSet();
            list = list.Where(i => ids.Contains(i.BlockId)).ToList();
        }
        if (blockId.HasValue)
            list = list.Where(i => i.BlockId == blockId.Value).ToList();

        list = list.OrderByDescending(i => i.InspectionDate).ToList();

        var blockNumber = blockId.HasValue
            ? blocks.FirstOrDefault(b => b.Id == blockId.Value)?.BlockNumber
            : null;

        var stream   = ExcelExportService.ExportInspections(list, dateFrom, dateTo, floor, blockNumber);
        var fileName = $"Проверки_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> ExportStudentRatings()
    {
        var ratingsResp = await _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        var ratings     = ratingsResp?.Data?.OrderByDescending(r => r.TotalPoints).ToList() ?? new();

        var stream   = ExcelExportService.ExportStudentRatings(ratings);
        var fileName = $"Рейтинг_студентов_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // ─── Расписание проверок ──────────────────────────────────────────────────

    public IActionResult Schedule()
    {
        ViewData["Title"] = "Расписание проверок";
        return View(new InspectionScheduleViewModel
        {
            EnabledDays = _schedule.EnabledDays.ToHashSet(),
        });
    }

    [HttpPost]
    public IActionResult Schedule(List<int>? enabledDays)
    {
        _schedule.UpdateSchedule(enabledDays ?? new List<int>());
        TempData["Success"] = "Расписание проверок обновлено";
        return RedirectToAction("Schedule");
    }
}
