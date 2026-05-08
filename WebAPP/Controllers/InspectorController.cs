using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers;

[Authorize(Roles = "Inspector")]
public class InspectorController : Controller
{
    private readonly ApiClient _api;
    private readonly InspectionScheduleService _schedule;

    public InspectorController(ApiClient api, InspectionScheduleService schedule)
    {
        _api = api;
        _schedule = schedule;
    }

    private int UserId => int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

    // ─── Главная страница (как у студента) ───────────────────────────────────

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Моя страница";
        var userId = UserId;

        // Сначала загружаем блоки отдельно — обязательно нужны для формы
        var blocksResp = await _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var blocks = blocksResp?.Data?.ToList() ?? new();

        // Остальные данные параллельно
        var residenceTask  = _api.GetAsync<ResidenceDto>($"api/residences/current/user/{userId}");
        var ratingTask     = _api.GetAsync<StudentRatingDto>($"api/studentpoints/ratings/{userId}");
        var requestsTask   = _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests/my");
        var allInspTask    = _api.GetAsync<IEnumerable<InspectionDto>>("api/inspections");
        await Task.WhenAll(residenceTask, ratingTask, requestsTask, allInspTask);

        InspectionDto? latestInspection = null;
        if (residenceTask.Result?.Data != null)
        {
            var blockId  = residenceTask.Result.Data.BlockId;
            var inspResp = await _api.GetAsync<IEnumerable<InspectionDto>>($"api/inspections/by-block/{blockId}");
            latestInspection = inspResp?.Data?.OrderByDescending(i => i.InspectionDate).FirstOrDefault();
        }

        var myInsp = allInspTask.Result?.Data?
            .Where(i => i.InspectorId == userId)
            .OrderByDescending(i => i.InspectionDate)
            .ToList() ?? new();

        var vm = new InspectorDashboardViewModel
        {
            Residence             = residenceTask.Result?.Data,
            LatestBlockInspection = latestInspection,
            Rating                = ratingTask.Result?.Data,
            MyRecentRequests      = requestsTask.Result?.Data?.OrderByDescending(r => r.CreatedAt).Take(5).ToList() ?? new(),
            MyInspections         = myInsp,
            Blocks                = blocks,
            IsInspectionDay       = _schedule.IsInspectionAllowedToday(),
        };
        return View(vm);
    }

    // ─── Заявки на ремонт ─────────────────────────────────────────────────────

    public async Task<IActionResult> RepairRequests()
    {
        ViewData["Title"] = "Заявки на ремонт";
        var userId = UserId;

        var residenceTask = _api.GetAsync<ResidenceDto>($"api/residences/current/user/{userId}");
        var requestsTask  = _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests/my");
        await Task.WhenAll(residenceTask, requestsTask);

        var vm = new InspectorRepairRequestsViewModel
        {
            Requests  = requestsTask.Result?.Data?.OrderByDescending(r => r.CreatedAt).ToList() ?? new(),
            Residence = residenceTask.Result?.Data,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRepairRequest(CreateRepairRequestDto dto)
    {
        var result = await _api.PostAsync<RepairRequestDto>("api/repairrequests", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Заявка подана" : result?.Message ?? "Ошибка при подаче заявки";
        return RedirectToAction("RepairRequests");
    }

    // ─── Мои баллы ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Points()
    {
        ViewData["Title"] = "Мои баллы";
        var userId = UserId;

        var ratingTask = _api.GetAsync<StudentRatingDto>($"api/studentpoints/ratings/{userId}");
        var pointsTask = _api.GetAsync<IEnumerable<StudentPointDto>>("api/studentpoints/my-points");
        await Task.WhenAll(ratingTask, pointsTask);

        var vm = new InspectorPointsViewModel
        {
            Rating = ratingTask.Result?.Data,
            Points = pointsTask.Result?.Data?.OrderByDescending(p => p.CreatedAt).ToList() ?? new(),
        };
        return View(vm);
    }

    // ─── Мероприятия ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Events()
    {
        ViewData["Title"] = "Мероприятия";

        var eventsResp = await _api.GetAsync<IEnumerable<EventDto>>("api/events");
        var all        = eventsResp?.Data?.ToList() ?? new();
        var today      = DateOnly.FromDateTime(DateTime.Today);

        var vm = new StudentEventsViewModel
        {
            UpcomingEvents = all.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            PastEvents     = all.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList(),
        };
        return View(vm);
    }

    // ─── Проверки (разблокированы в разрешённые дни) ─────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateInspection(CreateInspectionDto dto)
    {
        if (!_schedule.IsInspectionAllowedToday())
        {
            TempData["Error"] = "Сегодня проверки не проводятся. Обратитесь к воспитателю за расписанием.";
            return RedirectToAction("Dashboard");
        }

        // InspectorId устанавливается в WebAPI из JWT-токена
        var result = await _api.PostAsync<InspectionDto>("api/inspections", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Проверка сохранена" : result?.Message ?? "Ошибка";
        return RedirectToAction("Dashboard");
    }
}
