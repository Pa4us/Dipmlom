using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers;

[Authorize(Roles = "Mechanic")]
public class MechanicController : Controller
{
    private readonly ApiClient _api;
    public MechanicController(ApiClient api) => _api = api;

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Заявки на ремонт";
        var allTask      = _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests");
        var myTask       = _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests/assigned-to-me");
        await Task.WhenAll(allTask, myTask);

        var all = allTask.Result?.Data?.ToList() ?? new();
        var vm = new MechanicDashboardViewModel
        {
            // Новые — не назначены никому, статус Pending
            NewRequests = all.Where(r => r.AssignedToId == null && r.Status == "Pending")
                            .OrderByDescending(r => r.CreatedAt).ToList(),
            MyRequests  = myTask.Result?.Data?.OrderByDescending(r => r.CreatedAt).ToList() ?? new(),
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["Title"] = "Детали заявки";
        var reqTask      = _api.GetAsync<RepairRequestDto>($"api/repairrequests/{id}");
        var commentsTask = _api.GetAsync<IEnumerable<RepairCommentDto>>($"api/repairrequests/{id}/comments");
        await Task.WhenAll(reqTask, commentsTask);

        if (reqTask.Result?.Data == null) return NotFound();

        var vm = new RepairRequestDetailsViewModel
        {
            Request  = reqTask.Result.Data,
            Comments = commentsTask.Result?.Data?.OrderBy(c => c.CreatedAt).ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? comment)
    {
        var dto = new UpdateRepairRequestStatusDto { Id = id, Status = status, Comment = comment };
        var result = await _api.PatchAsync<RepairRequestDto>($"api/repairrequests/{id}/status", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? $"Статус изменён на «{StatusLabel(status)}»" : result?.Message ?? "Ошибка";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int id, string comment)
    {
        var dto = new AddCommentDto { RepairRequestId = id, Comment = comment };
        await _api.PostAsync<RepairCommentDto>($"api/repairrequests/{id}/comments", dto);
        return RedirectToAction("Details", new { id });
    }

    private static string StatusLabel(string s) => s switch
    {
        "InProgress" => "В работе",
        "Completed"  => "Выполнено",
        "Cancelled"  => "Отменено",
        _            => s
    };
}
