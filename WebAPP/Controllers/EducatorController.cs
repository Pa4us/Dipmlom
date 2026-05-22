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

    public async Task<IActionResult> Dashboard(
        string? dateFrom, string? dateTo, int? blockId, int? floor)
    {
        ViewData["Title"] = "Статистика";

        var blocksTask = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var inspTask   = _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        await Task.WhenAll(blocksTask, inspTask);

        var blocks         = blocksTask.Result?.Data?.ToList() ?? new();
        var allInspections = inspTask.Result?.Data?.ToList()   ?? new();

        // Применяем фильтры
        bool hasFilters = !string.IsNullOrEmpty(dateFrom) || !string.IsNullOrEmpty(dateTo)
                          || floor.HasValue || blockId.HasValue;

        var filtered = allInspections.AsEnumerable();
        if (!string.IsNullOrEmpty(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
            filtered = filtered.Where(i => i.InspectionDate >= from);
        if (!string.IsNullOrEmpty(dateTo)   && DateOnly.TryParse(dateTo,   out var to))
            filtered = filtered.Where(i => i.InspectionDate <= to);
        if (floor.HasValue)
        {
            var floorBlockIds = blocks.Where(b => b.Floor == floor.Value).Select(b => b.Id).ToHashSet();
            filtered = filtered.Where(i => floorBlockIds.Contains(i.BlockId));
        }
        if (blockId.HasValue)
            filtered = filtered.Where(i => i.BlockId == blockId.Value);

        var filteredList = filtered.OrderByDescending(i => i.InspectionDate).ToList();

        // Статистика: при фильтрах — считаем из отфильтрованных проверок;
        //             без фильтров — берём предрасчитанную (быстрее).
        DormitoryStatisticsDto stats;
        if (hasFilters)
            stats = BuildStatsFromInspections(filteredList, blocks);
        else
        {
            var statsResp = await _api.GetAsync<DormitoryStatisticsDto>("api/statistics/dormitory");
            stats = statsResp?.Data ?? new();
        }

        var allScores = stats.Floors.SelectMany(f => f.BlockScores)
                                    .OrderByDescending(b => b.Score).ToList();

        var vm = new EducatorDashboardViewModel
        {
            Stats             = stats,
            RecentInspections = filteredList.Take(10).ToList(),
            BestBlocks        = allScores.Take(3).ToList(),
            WorstBlocks       = allScores.TakeLast(3).ToList(),
            Blocks            = blocks,
            Floors            = blocks.Select(b => b.Floor).Distinct().OrderBy(f => f).ToList(),
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            SelectedBlockId   = blockId,
            SelectedFloor     = floor,
        };
        return View(vm);
    }

    /// <summary>
    /// Вычисляет статистику на лету из набора проверок (для режима с фильтрами).
    /// </summary>
    private static DormitoryStatisticsDto BuildStatsFromInspections(
        List<InspectionDto> inspections, List<BlockDto> blocks)
    {
        var byBlock = inspections
            .GroupBy(i => i.BlockId)
            .Select(g => new BlockWeeklyScoreDto
            {
                BlockId     = g.Key,
                BlockNumber = g.First().BlockNumber,
                Score       = (decimal)g.Average(i => i.Score),
            })
            .ToList();

        var floors = blocks
            .GroupBy(b => b.Floor)
            .Select(g =>
            {
                var blockScores = g
                    .Select(b => byBlock.FirstOrDefault(x => x.BlockId == b.Id))
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToList();
                return new FloorStatisticsDto
                {
                    Floor        = g.Key,
                    BlocksCount  = g.Count(),
                    AverageScore = blockScores.Any() ? blockScores.Average(b => b.Score) : 0,
                    BlockScores  = blockScores,
                };
            })
            .Where(f => f.BlockScores.Any())
            .ToList();

        return new DormitoryStatisticsDto
        {
            TotalBlocks  = blocks.Count,
            AverageScore = byBlock.Any() ? byBlock.Average(b => b.Score) : 0,
            Floors       = floors,
        };
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

        var studentsTask   = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Student");
        var inspectorsTask = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Inspector");
        var ratingsTask    = _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        var eventsTask     = _api.GetAsync<IEnumerable<EventDto>>("api/events");
        var blocksTask     = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var inspTask       = _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        await Task.WhenAll(studentsTask, inspectorsTask, ratingsTask, eventsTask, blocksTask, inspTask);

        var students = (studentsTask.Result?.Data ?? Enumerable.Empty<UserDto>())
            .Concat(inspectorsTask.Result?.Data ?? Enumerable.Empty<UserDto>())
            .OrderBy(u => u.FullName)
            .ToList();
        var blocks   = blocksTask.Result?.Data?.ToList() ?? new();

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

        // Санитарное состояние: блоки с последней оценкой < 6
        var allInspections = inspTask.Result?.Data?.ToList() ?? new();
        var lastByBlock = allInspections
            .GroupBy(i => i.BlockId)
            .Select(g => g.OrderByDescending(i => i.InspectionDate).First())
            .Where(i => i.Score < 6)
            .ToList();

        var sanitaryBlocks = new List<SanitaryBlockInfo>();
        foreach (var insp in lastByBlock)
        {
            var block = blocks.FirstOrDefault(b => b.Id == insp.BlockId);
            if (block == null) continue;
            var residentsResp = await _api.GetAsync<IEnumerable<UserDto>>($"api/users/by-block/{insp.BlockId}");
            sanitaryBlocks.Add(new SanitaryBlockInfo
            {
                Block              = block,
                LastScore          = insp.Score,
                LastInspectionDate = insp.InspectionDate,
                Residents          = residentsResp?.Data?.ToList() ?? new(),
            });
        }

        var vm = new PointsViewModel
        {
            Ratings           = ratingsTask.Result?.Data?.OrderByDescending(r => r.TotalPoints).ToList() ?? new(),
            Students          = students,
            PastEvents        = pastEvents,
            EventParticipants = participantMap,
            SanitaryBlocks    = sanitaryBlocks.OrderBy(b => b.LastScore).ToList(),
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

    /// <summary>
    /// Взыскивает баллы у ВСЕХ жильцов блока (санитарное состояние).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeductSanitaryPoints(int blockId, int points, string reason)
    {
        var residentsResp = await _api.GetAsync<IEnumerable<UserDto>>($"api/users/by-block/{blockId}");
        var residents     = residentsResp?.Data?.ToList() ?? new();

        if (!residents.Any())
        {
            TempData["Error"] = "Жильцы блока не найдены";
            return RedirectToAction("Points");
        }

        int success = 0;
        foreach (var resident in residents)
        {
            var dto = new DeductPointsDto
            {
                UserId     = resident.Id,
                Points     = points,
                Reason     = reason,
                SourceType = "Sanitary",
            };
            var result = await _api.PostAsync<StudentPointDto>("api/studentpoints/deduct", dto);
            if (result?.Success == true) success++;
        }

        TempData[success > 0 ? "Success" : "Error"] = success > 0
            ? $"Взыскано {points} б. у {success} из {residents.Count} жильцов блока"
            : "Не удалось взыскать баллы";

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

    // ─── Заселение — обработка списка ────────────────────────────────────────

    public async Task<IActionResult> CheckIn()
    {
        ViewData["Title"] = "Заселение";
        var resp = await _api.GetAsync<IEnumerable<CheckInRequestItemDto>>("api/checkin/pending-items");
        var vm = new EducatorCheckInViewModel
        {
            PendingItems = resp?.Data?.ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessSelectedCheckIn(List<int> itemIds)
    {
        if (itemIds == null || !itemIds.Any())
        {
            TempData["Error"] = "Выберите хотя бы одного студента";
            return RedirectToAction("CheckIn");
        }

        var educatorId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var resp = await _api.PostAsync<ProcessCheckInResultDto>(
            "api/checkin/items/process",
            new { EducatorId = educatorId, ItemIds = itemIds });

        if (resp?.Success != true || resp.Data == null)
        {
            TempData["Error"] = resp?.Message ?? "Ошибка при заселении";
            return RedirectToAction("CheckIn");
        }

        var vm = new EducatorCheckInResultViewModel
        {
            CreatedItems   = resp.Data.CreatedItems,
            ProcessedCount = resp.Data.ProcessedCount,
        };
        return View("CheckInResult", vm);
    }

    [HttpPost]
    public async Task<IActionResult> RejectSelectedCheckIn(List<int> itemIds)
    {
        if (itemIds == null || !itemIds.Any())
        {
            TempData["Error"] = "Выберите хотя бы одного студента";
            return RedirectToAction("CheckIn");
        }

        var resp = await _api.PostAsync<int>(
            "api/checkin/items/reject",
            new { ItemIds = itemIds });

        TempData[resp?.Success == true ? "Success" : "Error"] =
            resp?.Success == true
                ? $"Отклонено студентов: {resp.Data}"
                : resp?.Message ?? "Ошибка при отклонении";

        return RedirectToAction("CheckIn");
    }

    /// <summary>
    /// Скачивает сводный PDF с паролями всех студентов,
    /// заселённых за последние 4 дня (пока пароли ещё хранятся).
    /// </summary>
    public async Task<IActionResult> DownloadRecentCredentialsPdf()
    {
        var resp = await _api.GetAsync<IEnumerable<CheckInRequestItemDto>>(
            "api/checkin/recently-checked-in?days=4");

        var items = resp?.Data?.ToList() ?? new();

        if (!items.Any())
        {
            TempData["Error"] = "Нет заселённых студентов за последние 4 дня";
            return RedirectToAction("CheckIn");
        }

        var stream = PdfExportService.ExportCheckInCredentialsPdf(items);
        return File(stream, "application/pdf",
            $"Пароли_за_{DateTime.Today:yyyyMMdd}.pdf");
    }

    public IActionResult DownloadCheckInCredentialsPdf(string dataJson)
    {
        List<CheckInRequestItemDto>? items;
        try { items = System.Text.Json.JsonSerializer.Deserialize<List<CheckInRequestItemDto>>(dataJson); }
        catch { TempData["Error"] = "Ошибка формирования PDF"; return RedirectToAction("CheckIn"); }

        if (items == null || !items.Any())
        { TempData["Error"] = "Нет данных для PDF"; return RedirectToAction("CheckIn"); }

        var stream = PdfExportService.ExportCheckInCredentialsPdf(items);
        return File(stream, "application/pdf", $"Учётные_данные_{DateTime.Today:yyyyMMdd}.pdf");
    }

    // ─── Выселение — подача от воспитателя ───────────────────────────────────

    public async Task<IActionResult> Eviction(string? search)
    {
        ViewData["Title"] = "Выселение";

        var studentsTask   = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Student");
        var inspectorsTask = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role-name/Inspector");
        var residencesTask = _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        var ratingsTask    = _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        var evictionsTask  = _api.GetAsync<IEnumerable<EvictionRequestDto>>("api/eviction/pending");
        await Task.WhenAll(studentsTask, inspectorsTask, residencesTask, ratingsTask, evictionsTask);

        // Проверяющие тоже являются студентами-жильцами → объединяем оба списка
        var students = (studentsTask.Result?.Data ?? Enumerable.Empty<UserDto>())
            .Concat(inspectorsTask.Result?.Data ?? Enumerable.Empty<UserDto>())
            .ToList();
        var residenceMap = residencesTask.Result?.Data?
            .Where(r => r.IsCurrent)
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<int, ResidenceDto>();
        var ratingMap = ratingsTask.Result?.Data?
            .ToDictionary(r => r.UserId)
            ?? new Dictionary<int, StudentRatingDto>();

        // Только студенты с текущим проживанием
        var residents = students
            .Where(s => residenceMap.ContainsKey(s.Id))
            .OrderBy(s => s.FullName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            residents = residents.Where(s =>
                s.FullName.ToLower().Contains(q) ||
                s.Username.ToLower().Contains(q)).ToList();
        }

        // Pending — уже поданные на выселение
        var pendingIds = (evictionsTask.Result?.Data?.Select(e => e.UserId) ?? Enumerable.Empty<int>()).ToHashSet();

        var cards = residents.Select(s => new StudentCardDto
        {
            User      = s,
            Residence = residenceMap.GetValueOrDefault(s.Id),
            Rating    = ratingMap.GetValueOrDefault(s.Id),
        }).ToList();

        var vm = new EducatorEvictionViewModel
        {
            Residents        = cards,
            PendingEvictions = evictionsTask.Result?.Data?.ToList() ?? new(),
            Search           = search,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitEviction(List<int> userIds)
    {
        if (userIds == null || !userIds.Any())
        { TempData["Error"] = "Выберите хотя бы одного студента"; return RedirectToAction("Eviction"); }

        var educatorId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var resp = await _api.PostAsync<IEnumerable<EvictionRequestDto>>("api/eviction/requests",
            new CreateEvictionRequestDto { EducatorId = educatorId, UserIds = userIds });

        TempData[resp?.Success == true ? "Success" : "Error"] =
            resp?.Success == true
                ? $"Обходные листы подтверждены для {userIds.Count} студентов"
                : resp?.Message ?? "Ошибка";
        return RedirectToAction("Eviction");
    }

    public async Task<IActionResult> ExportInspectionsPdf(
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

        var stream   = PdfExportService.ExportInspectionsPdf(list, dateFrom, dateTo, floor, blockNumber);
        var fileName = $"Проверки_{DateTime.Today:yyyyMMdd}.pdf";
        return File(stream, "application/pdf", fileName);
    }

    public async Task<IActionResult> ExportStudentRatingsPdf()
    {
        var ratingsResp = await _api.GetAsync<IEnumerable<StudentRatingDto>>("api/studentpoints/ratings");
        var ratings     = ratingsResp?.Data?.OrderByDescending(r => r.TotalPoints).ToList() ?? new();

        var stream   = PdfExportService.ExportStudentRatingsPdf(ratings);
        var fileName = $"Рейтинг_студентов_{DateTime.Today:yyyyMMdd}.pdf";
        return File(stream, "application/pdf", fileName);
    }

    // ─── Расписание проверок ──────────────────────────────────────────────────

    public IActionResult Schedule()
    {
        ViewData["Title"] = "Расписание проверок";
        return View(new InspectionScheduleViewModel
        {
            SelectedDay = _schedule.SelectedDay,
            MonthDates  = _schedule.GetMonthSchedule(),
        });
    }

    [HttpPost]
    public IActionResult Schedule(int selectedDay)
    {
        _schedule.UpdateSchedule(selectedDay);
        TempData["Success"] = "Расписание проверок обновлено";
        return RedirectToAction("Schedule");
    }
}
