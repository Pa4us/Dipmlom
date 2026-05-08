using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Text.Json;
using WebAPP.Models.ViewModels;
using WebAPP.Services;

namespace WebAPP.Controllers;

[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly ApiClient _api;
    public ManagerController(ApiClient api) => _api = api;

    // ─── Дашборд ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Панель заведующей";

        var usersTask      = _api.GetAsync<IEnumerable<UserDto>>("api/users");
        var blocksTask     = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var roomsTask      = _api.GetAsync<IEnumerable<RoomDto>>("api/rooms");
        var residencesTask = _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        await Task.WhenAll(usersTask, blocksTask, roomsTask, residencesTask);

        var users      = usersTask.Result?.Data?.ToList()      ?? new();
        var rooms      = roomsTask.Result?.Data?.ToList()      ?? new();
        var residences = residencesTask.Result?.Data?.ToList() ?? new();

        var vm = new ManagerDashboardViewModel
        {
            TotalUsers       = users.Count,
            TotalStudents    = users.Count(u => u.RoleName == "Student"),
            TotalBlocks      = blocksTask.Result?.Data?.Count() ?? 0,
            TotalRooms       = rooms.Count,
            FreeRooms        = rooms.Count(r => r.FreePlaces > 0),
            CurrentResidents = residences.Count(r => r.IsCurrent),
            RecentUsers      = users.OrderByDescending(u => u.CreatedAt).Take(6).ToList(),
        };
        return View(vm);
    }

    // ─── Аккаунты ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Users(string? search)
    {
        ViewData["Title"] = "Управление аккаунтами";

        var usersTask = _api.GetAsync<IEnumerable<UserDto>>("api/users");
        var rolesTask = _api.GetAsync<IEnumerable<RoleDto>>("api/roles");
        await Task.WhenAll(usersTask, rolesTask);

        var users = usersTask.Result?.Data?.ToList() ?? new();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            users = users.Where(u =>
                u.FullName.ToLower().Contains(q) ||
                u.Username.ToLower().Contains(q) ||
                u.Email.ToLower().Contains(q)).ToList();
        }

        var vm = new ManagerUsersViewModel
        {
            Users  = users.OrderBy(u => u.RoleName).ThenBy(u => u.FullName).ToList(),
            Roles  = rolesTask.Result?.Data?.OrderBy(r => r.Name).ToList() ?? new(),
            Search = search,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        var result = await _api.PostAsync<UserDto>("api/users", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? $"Пользователь «{dto.FullName}» создан" : result?.Message ?? "Ошибка при создании";
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(UpdateUserDto dto)
    {
        var result = await _api.PutAsync<UserDto>("api/users", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Данные пользователя сохранены" : result?.Message ?? "Ошибка при сохранении";
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> ResetUserPassword(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Пароль должен содержать не менее 6 символов";
            return RedirectToAction("Users");
        }
        var result = await _api.PostAsync<bool>($"api/users/{userId}/reset-password", new { NewPassword = newPassword });
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Пароль пользователя сброшен" : result?.Message ?? "Ошибка при сбросе пароля";
        return RedirectToAction("Users");
    }

    // ─── Заявки на ремонт ────────────────────────────────────────────────────

    public async Task<IActionResult> RepairRequests(string? status)
    {
        ViewData["Title"] = "Заявки на ремонт";

        var resp = await _api.GetAsync<IEnumerable<RepairRequestDto>>("api/repairrequests");
        var all  = resp?.Data?.ToList() ?? new();

        var filtered = string.IsNullOrEmpty(status)
            ? all
            : all.Where(r => r.Status == status).ToList();

        var vm = new ManagerRepairRequestsViewModel
        {
            Requests     = filtered.OrderByDescending(r => r.CreatedAt).ToList(),
            StatusFilter = status,
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRepairRequest(int id, string? status)
    {
        var result = await _api.DeleteAsync<bool>($"api/repairrequests/{id}");
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Заявка удалена" : result?.Message ?? "Ошибка при удалении";
        return RedirectToAction("RepairRequests", new { status });
    }

    // ─── Блоки ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Blocks()
    {
        ViewData["Title"] = "Блоки";

        var blocksTask = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        var roomsTask  = _api.GetAsync<IEnumerable<RoomDto>>("api/rooms");
        await Task.WhenAll(blocksTask, roomsTask);

        var vm = new ManagerBlocksViewModel
        {
            Blocks   = blocksTask.Result?.Data?.OrderBy(b => b.Floor).ThenBy(b => b.BlockIndex).ToList() ?? new(),
            AllRooms = roomsTask.Result?.Data?.ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBlock(CreateBlockDto dto)
    {
        var result = await _api.PostAsync<BlockDto>("api/blocks", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? $"Блок «{dto.BlockNumber}» создан" : result?.Message ?? "Ошибка";
        return RedirectToAction("Blocks");
    }

    [HttpPost]
    public async Task<IActionResult> EditBlock(UpdateBlockDto dto)
    {
        var result = await _api.PutAsync<BlockDto>("api/blocks", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Блок обновлён" : result?.Message ?? "Ошибка";
        return RedirectToAction("Blocks");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBlock(int id)
    {
        var result = await _api.DeleteAsync<bool>($"api/blocks/{id}");
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Блок удалён" : result?.Message ?? "Ошибка";
        return RedirectToAction("Blocks");
    }

    // ─── Комнаты ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Rooms()
    {
        ViewData["Title"] = "Комнаты";

        var roomsTask  = _api.GetAsync<IEnumerable<RoomDto>>("api/rooms");
        var blocksTask = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        await Task.WhenAll(roomsTask, blocksTask);

        var vm = new ManagerRoomsViewModel
        {
            Rooms  = roomsTask.Result?.Data?.OrderBy(r => r.BlockNumber).ThenBy(r => r.RoomNumber).ToList() ?? new(),
            Blocks = blocksTask.Result?.Data?.OrderBy(b => b.Floor).ThenBy(b => b.BlockIndex).ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom(CreateRoomDto dto)
    {
        var result = await _api.PostAsync<RoomDto>("api/rooms", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? $"Комната «{dto.RoomNumber}» создана" : result?.Message ?? "Ошибка";
        return RedirectToAction("Rooms");
    }

    [HttpPost]
    public async Task<IActionResult> EditRoom(UpdateRoomDto dto)
    {
        var result = await _api.PutAsync<RoomDto>("api/rooms", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Комната обновлена" : result?.Message ?? "Ошибка";
        return RedirectToAction("Rooms");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var result = await _api.DeleteAsync<bool>($"api/rooms/{id}");
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Комната удалена" : result?.Message ?? "Ошибка";
        return RedirectToAction("Rooms");
    }

    // ─── Проживание ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Residences()
    {
        ViewData["Title"] = "Проживание";

        var residencesTask = _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        var studentsTask   = _api.GetAsync<IEnumerable<UserDto>>("api/users/by-role/1");
        var roomsTask      = _api.GetAsync<IEnumerable<RoomDto>>("api/rooms");
        var blocksTask     = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        await Task.WhenAll(residencesTask, studentsTask, roomsTask, blocksTask);

        var residences = residencesTask.Result?.Data?.ToList() ?? new();
        var students   = studentsTask.Result?.Data?.ToList()   ?? new();
        var rooms      = roomsTask.Result?.Data?.ToList()      ?? new();

        var residentIds       = residences.Where(r => r.IsCurrent).Select(r => r.UserId).ToHashSet();
        var availableStudents = students.Where(s => !residentIds.Contains(s.Id)).OrderBy(s => s.FullName).ToList();

        var vm = new ManagerResidencesViewModel
        {
            CurrentResidences = residences.Where(r => r.IsCurrent)
                                          .OrderBy(r => r.BlockNumber).ThenBy(r => r.RoomNumber).ToList(),
            Students = availableStudents,
            Rooms    = rooms.OrderBy(r => r.BlockNumber).ThenBy(r => r.RoomNumber).ToList(),
            Blocks   = blocksTask.Result?.Data?.OrderBy(b => b.Floor).ThenBy(b => b.BlockIndex).ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(CreateResidenceDto dto)
    {
        var result = await _api.PostAsync<ResidenceDto>("api/residences", dto);
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Студент заселён" : result?.Message ?? "Ошибка при заселении";
        return RedirectToAction("Residences");
    }

    // ─── Импорт аккаунтов из Excel ───────────────────────────────────────────

    [HttpGet]
    public IActionResult ImportUsers()
    {
        ViewData["Title"] = "Импорт аккаунтов";
        return View();
    }

    /// <summary>Шаг 1 — загружаем файл, парсим, отправляем на preview (dry-run)</summary>
    [HttpPost]
    public async Task<IActionResult> ImportUsers(IFormFile? file)
    {
        ViewData["Title"] = "Импорт аккаунтов";

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Выберите файл Excel (.xlsx)");
            return View();
        }
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Поддерживается только формат .xlsx");
            return View();
        }

        // Парсим Excel в DTO
        using var stream = file.OpenReadStream();
        var (rows, parseError) = ExcelImportService.ParseUsersFile(stream);
        if (parseError != null)
        {
            ModelState.AddModelError("", parseError);
            return View();
        }

        // Dry-run: валидация + генерация паролей
        var resp = await _api.PostAsync<ImportUsersResultDto>(
            "api/users/import",
            new ImportUsersRequestDto { Rows = rows, DryRun = true });

        if (resp?.Success != true || resp.Data == null)
        {
            ModelState.AddModelError("", resp?.Message ?? "Ошибка при проверке файла");
            return View();
        }

        var vm = new ImportUsersPreviewViewModel
        {
            Result       = resp.Data,
            ValidRowsJson = JsonSerializer.Serialize(resp.Data.ValidRows),
        };
        return View("ImportUsersPreview", vm);
    }

    /// <summary>Шаг 2 — подтверждение, реальное создание аккаунтов</summary>
    [HttpPost]
    public async Task<IActionResult> ImportUsersConfirm(string validRowsJson)
    {
        ViewData["Title"] = "Импорт аккаунтов";

        List<ImportUserRowDto>? rows;
        try { rows = JsonSerializer.Deserialize<List<ImportUserRowDto>>(validRowsJson); }
        catch { TempData["Error"] = "Не удалось обработать данные. Попробуйте снова."; return RedirectToAction("ImportUsers"); }

        if (rows == null || rows.Count == 0)
        { TempData["Error"] = "Нет строк для импорта."; return RedirectToAction("ImportUsers"); }

        var resp = await _api.PostAsync<ImportUsersResultDto>(
            "api/users/import",
            new ImportUsersRequestDto { Rows = rows, DryRun = false });

        if (resp?.Success != true || resp.Data == null)
        {
            TempData["Error"] = resp?.Message ?? "Ошибка при создании аккаунтов";
            return RedirectToAction("ImportUsers");
        }

        // Генерируем Excel с паролями для скачивания
        var passwordStream = ExcelExportService.ExportImportedPasswords(resp.Data.ValidRows);
        var base64 = Convert.ToBase64String(((MemoryStream)passwordStream).ToArray());

        var vm = new ImportUsersResultViewModel
        {
            Result              = resp.Data,
            PasswordReportBase64 = base64,
        };
        return View("ImportUsersResult", vm);
    }

    /// <summary>Шаблон Excel для импорта аккаунтов</summary>
    public IActionResult ImportUsersTemplate()
    {
        var stream   = ExcelExportService.ExportImportUsersTemplate();
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Шаблон_импорта_аккаунтов.xlsx");
    }

    public async Task<IActionResult> ExportResidences()
    {
        var residencesResp = await _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        var residences = residencesResp?.Data?
            .Where(r => r.IsCurrent)
            .OrderBy(r => r.BlockNumber).ThenBy(r => r.RoomNumber)
            .ToList() ?? new();

        var stream   = ExcelExportService.ExportResidences(residences);
        var fileName = $"Проживание_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(int id)
    {
        var result = await _api.PostAsync<ResidenceDto>(
            $"api/residences/{id}/checkout",
            new { MoveOutDate = DateOnly.FromDateTime(DateTime.Today) });
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Студент выселен" : result?.Message ?? "Ошибка при выселении";
        return RedirectToAction("Residences");
    }
}
