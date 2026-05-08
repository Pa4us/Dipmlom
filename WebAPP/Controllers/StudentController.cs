using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApiClient _api;
    public StudentController(ApiClient api) => _api = api;

    private int UserId => int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

    // ─── Дашборд ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Моя страница";
        var userId = UserId;

        var residenceTask = _api.GetAsync<ResidenceDto>($"api/residences/current/user/{userId}");
        var ratingTask    = _api.GetAsync<StudentRatingDto>($"api/studentpoints/ratings/{userId}");
        var requestsTask  = _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests/my");
        var eventsTask    = _api.GetAsync<IEnumerable<EventDto>>("api/events/upcoming");
        await Task.WhenAll(residenceTask, ratingTask, requestsTask, eventsTask);

        InspectionDto? latestInspection = null;
        if (residenceTask.Result?.Data != null)
        {
            var blockId  = residenceTask.Result.Data.BlockId;
            var inspResp = await _api.GetAsync<IEnumerable<InspectionDto>>($"api/inspections/by-block/{blockId}");
            latestInspection = inspResp?.Data?.OrderByDescending(i => i.InspectionDate).FirstOrDefault();
        }

        var vm = new StudentDashboardViewModel
        {
            Residence             = residenceTask.Result?.Data,
            LatestBlockInspection = latestInspection,
            Rating                = ratingTask.Result?.Data,
            MyRecentRequests      = requestsTask.Result?.Data?.OrderByDescending(r => r.CreatedAt).Take(5).ToList() ?? new(),
            UpcomingEvents        = eventsTask.Result?.Data?.OrderBy(e => e.EventDate).Take(3).ToList() ?? new(),
        };
        return View(vm);
    }

    // ─── Мероприятия ──────────────────────────────────────────────────────────

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

    // ─── Заявки на ремонт ─────────────────────────────────────────────────────

    public async Task<IActionResult> RepairRequests()
    {
        ViewData["Title"] = "Мои заявки на ремонт";
        var userId = UserId;

        var residenceResp = await _api.GetAsync<ResidenceDto>($"api/residences/current/user/{userId}");
        var requestsResp  = await _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests/my");

        var vm = new StudentRepairRequestsViewModel
        {
            Requests  = requestsResp?.Data?.OrderByDescending(r => r.CreatedAt).ToList() ?? new(),
            Residence = residenceResp?.Data,
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

        var vm = new StudentPointsViewModel
        {
            Rating = ratingTask.Result?.Data,
            Points = pointsTask.Result?.Data?.OrderByDescending(p => p.CreatedAt).ToList() ?? new(),
        };
        return View(vm);
    }
}
