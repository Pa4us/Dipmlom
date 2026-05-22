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

    public async Task<IActionResult> Users(string? search, string? role)
    {
        ViewData["Title"] = "Управление аккаунтами";

        var usersTask = _api.GetAsync<IEnumerable<UserDto>>("api/users");
        var rolesTask = _api.GetAsync<IEnumerable<RoleDto>>("api/roles");
        await Task.WhenAll(usersTask, rolesTask);

        var users = usersTask.Result?.Data?.ToList() ?? new();

        // Фильтр по роли
        if (!string.IsNullOrWhiteSpace(role))
            users = users.Where(u => u.RoleName == role).ToList();

        // Поиск по тексту
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
            Users        = users.OrderBy(u => u.RoleName).ThenBy(u => u.FullName).ToList(),
            Roles        = rolesTask.Result?.Data?.OrderBy(r => r.Name).ToList() ?? new(),
            Search       = search,
            SelectedRole = role,
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

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _api.DeleteAsync<bool>($"api/users/{id}");
        TempData[result?.Success == true ? "Success" : "Error"] =
            result?.Success == true ? "Аккаунт удалён" : result?.Message ?? "Ошибка при удалении";
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
        var usersTask      = _api.GetAsync<IEnumerable<UserDto>>("api/users");
        var roomsTask      = _api.GetAsync<IEnumerable<RoomDto>>("api/rooms");
        var blocksTask     = _api.GetAsync<IEnumerable<BlockDto>>("api/blocks");
        await Task.WhenAll(residencesTask, usersTask, roomsTask, blocksTask);

        var residences = residencesTask.Result?.Data?.ToList() ?? new();
        // Проживать в общежитии могут студенты и проверяющие
        var students   = usersTask.Result?.Data?
                             .Where(u => u.RoleName == "Student" || u.RoleName == "Inspector")
                             .ToList() ?? new();
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

    public async Task<IActionResult> ExportResidences()
    {
        var residencesResp = await _api.GetAsync<IEnumerable<ResidenceDto>>("api/residences");
        var residences = residencesResp?.Data?
            .Where(r => r.IsCurrent)
            .OrderBy(r => r.BlockNumber).ThenBy(r => r.RoomNumber)
            .ToList() ?? new();

        var stream   = PdfExportService.ExportResidencesPdf(residences);
        var fileName = $"Проживание_{DateTime.Today:yyyyMMdd}.pdf";
        return File(stream, "application/pdf", fileName);
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

    // ─── Заселение — загрузка списка студентов ───────────────────────────────

    [HttpGet]
    public IActionResult CheckInUpload()
    {
        ViewData["Title"] = "Заселение — загрузка списка";
        return View();
    }

    [HttpPost]
    public IActionResult CheckInUpload(IFormFile? file)
    {
        ViewData["Title"] = "Заселение — загрузка списка";

        if (file == null || file.Length == 0)
        { ModelState.AddModelError("", "Выберите PDF-файл (.pdf)"); return View(); }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        { ModelState.AddModelError("", "Поддерживается только формат .pdf"); return View(); }

        using var stream = file.OpenReadStream();
        var (rows, error) = PdfImportService.ParseCheckInFile(stream);
        if (error != null)
        { ModelState.AddModelError("", error); return View(); }

        // Шаг 1: базовые проверки (ФИО, блок, номер комнаты)
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FullName))
                row.Error = "Не указано ФИО";
            else if (string.IsNullOrWhiteSpace(row.BlockNumber))
                row.Error = "Не указан блок";
            else if (!IsRoomNumberValid(row.BlockNumber))
                row.Error = $"Недопустимый номер комнаты в «{row.BlockNumber}» — допустимы только 1 или 2";
        }

        // Шаг 2: проверка вместимости — не более 2 студентов на одну комнату в файле
        var roomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Where(r => r.Error == null))
        {
            var key = row.BlockNumber.Trim().ToUpper();
            roomCounts.TryGetValue(key, out var cnt);
            if (cnt >= 2)
                row.Error = $"Комната «{row.BlockNumber}» уже занята 2 студентами — превышена вместимость";
            else
                roomCounts[key] = cnt + 1;
        }

        var valid   = rows.Where(r => r.Error == null).ToList();
        var invalid = rows.Where(r => r.Error != null).ToList();

        var vm = new CheckInUploadPreviewViewModel
        {
            ValidRows    = valid,
            InvalidRows  = invalid,
            ValidRowsJson = JsonSerializer.Serialize(valid),
        };
        return View("CheckInUploadPreview", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CheckInUploadConfirm(string validRowsJson)
    {
        List<CheckInItemRowDto>? rows;
        try { rows = JsonSerializer.Deserialize<List<CheckInItemRowDto>>(validRowsJson); }
        catch { TempData["Error"] = "Ошибка данных. Попробуйте снова."; return RedirectToAction("CheckInUpload"); }

        if (rows == null || rows.Count == 0)
        { TempData["Error"] = "Нет строк для заселения."; return RedirectToAction("CheckInUpload"); }

        var managerId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var resp = await _api.PostAsync<CheckInRequestDto>("api/checkin/requests",
            new CreateCheckInRequestDto { ManagerId = managerId, Items = rows });

        if (resp?.Success != true)
        { TempData["Error"] = resp?.Message ?? "Ошибка при создании заявки"; return RedirectToAction("CheckInUpload"); }

        TempData["Success"] = $"Заявка на заселение отправлена воспитателю ({rows.Count} студентов)";
        return RedirectToAction("CheckInUpload");
    }

    /// <summary>
    /// Скачивает тестовый PDF: 108 студентов по всем 9 этажам (3 блока × 4 студента).
    /// ВРЕМЕННАЯ КНОПКА — удалить в финальной версии.
    /// </summary>
    public IActionResult DownloadCheckInSamplePdf()
    {
        var rows   = GenerateSampleCheckInRows();
        var stream = PdfExportService.ExportCheckInListPdf(rows);
        return File(stream, "application/pdf", $"Тест_заселение_{DateTime.Today:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Генерирует 108 строк: 9 этажей × 3 блока × 2 комнаты × 2 студента.
    /// Имена, отчества и фамилии берутся из фиксированных массивов с шагом,
    /// чтобы избежать повторов и получить реалистичный список.
    /// </summary>
    private static List<CheckInItemRowDto> GenerateSampleCheckInRows()
    {
        string[] mLastNames = {
            "Абрамов","Александров","Андреев","Антонов","Белов","Борисов","Васильев","Волков",
            "Громов","Данилов","Дмитриев","Егоров","Жуков","Зайцев","Иванов","Игнатов","Ильин",
            "Карпов","Козлов","Крылов","Кузнецов","Лебедев","Лукьянов","Макаров","Михайлов",
            "Морозов","Никитин","Новиков","Орлов","Павлов","Петров","Попов","Романов","Семёнов",
            "Сидоров","Смирнов","Соколов","Соловьёв","Степанов","Тихонов","Ульянов","Фёдоров",
            "Фролов","Харитонов","Чернов","Щербаков","Яковлев",
        };
        string[] fLastNames = {
            "Абрамова","Александрова","Андреева","Белова","Борисова","Васильева","Волкова",
            "Григорьева","Дмитриева","Егорова","Жукова","Зайцева","Иванова","Игнатова","Ильина",
            "Карпова","Козлова","Кузнецова","Лебедева","Макарова","Михайлова","Морозова",
            "Никитина","Новикова","Орлова","Павлова","Петрова","Попова","Романова","Семёнова",
            "Сидорова","Смирнова","Соколова","Степанова","Тимофеева","Ульянова","Фёдорова",
            "Харитонова","Цветкова","Чернова","Шестакова","Щербакова",
        };
        string[] mFirstNames = {
            "Александр","Алексей","Андрей","Антон","Артём","Борис","Вадим","Виктор","Владимир",
            "Владислав","Дмитрий","Евгений","Иван","Игорь","Илья","Кирилл","Константин","Максим",
            "Михаил","Никита","Николай","Олег","Павел","Роман","Руслан","Сергей","Тимур","Юрий",
        };
        string[] fFirstNames = {
            "Алина","Анастасия","Анна","Виктория","Дарья","Екатерина","Елена","Ирина","Карина",
            "Ксения","Людмила","Маргарита","Мария","Надежда","Наталья","Оксана","Ольга","Полина",
            "Светлана","Татьяна","Юлия","Яна",
        };
        string[] mPatronymics = {
            "Александрович","Алексеевич","Андреевич","Антонович","Борисович","Васильевич",
            "Викторович","Владимирович","Дмитриевич","Евгеньевич","Иванович","Игоревич",
            "Ильич","Кириллович","Константинович","Максимович","Михайлович","Николаевич",
            "Олегович","Павлович","Романович","Сергеевич","Тимурович","Юрьевич",
        };
        string[] fPatronymics = {
            "Александровна","Алексеевна","Андреевна","Антоновна","Борисовна","Васильевна",
            "Викторовна","Владимировна","Дмитриевна","Евгеньевна","Ивановна","Игоревна",
            "Кирилловна","Константиновна","Максимовна","Михайловна","Николаевна","Олеговна",
            "Павловна","Романовна","Сергеевна","Тимуровна","Юрьевна",
        };

        string[] faculties = {
            "Машиностроительный факультет",
            "Механико-технологический факультет",
            "Факультет автоматизированных и информационных систем",
            "Энергетический факультет",
            "Гуманитарно-экономический факультет",
        };
        string[][] groups = {
            new[] { "МТ-11","МТ-12","МТ-13","МТ-21","МТ-22","МТ-23" },
            new[] { "ТМ-11","ТМ-12","ТМ-13","ТМ-21","ТМ-22","ТМ-23" },
            new[] { "ПО-11","ПО-12","ПО-13","ПО-21","ПО-22","ПО-23" },
            new[] { "ЭЭ-11","ЭЭ-12","ЭЭ-13","ЭЭ-21","ЭЭ-22","ЭЭ-23" },
            new[] { "ГЭ-11","ГЭ-12","ГЭ-13","ГЭ-21","ГЭ-22","ГЭ-23" },
        };

        var rows   = new List<CheckInItemRowDto>();
        int rowNum = 1;
        // Индексы для мужских / женских имён — шаг взаимно простой с длиной массива → нет повторов
        int mi = 0, fi = 0, mfi = 0, ffi = 0, mpi = 0, fpi = 0;

        for (int floor = 1; floor <= 9; floor++)
        {
            for (int block = 1; block <= 3; block++)
            {
                for (int room = 1; room <= 2; room++)
                {
                    var blockRoom = $"{floor}{block}-{room}";
                    for (int s = 0; s < 2; s++)
                    {
                        int fIdx  = (rowNum - 1) % 5;
                        int gIdx  = ((rowNum - 1) / 5) % 6;
                        bool male = rowNum % 2 == 1;

                        string fullName = male
                            ? $"{mLastNames[mi % mLastNames.Length]} {mFirstNames[mfi % mFirstNames.Length]} {mPatronymics[mpi % mPatronymics.Length]}"
                            : $"{fLastNames[fi % fLastNames.Length]} {fFirstNames[ffi % fFirstNames.Length]} {fPatronymics[fpi % fPatronymics.Length]}";

                        if (male) { mi += 3; mfi += 7; mpi += 11; }
                        else      { fi += 3; ffi += 7; fpi += 11; }

                        // Тестовый телефон
                        var phoneNumber = $"+375 ({(29 + rowNum % 3 == 29 ? "29" : rowNum % 3 == 1 ? "33" : "44")}) {100 + rowNum * 7 % 900:D3}-{10 + rowNum * 13 % 90:D2}-{10 + rowNum * 17 % 90:D2}";

                        rows.Add(new CheckInItemRowDto
                        {
                            RowNumber   = rowNum++,
                            FullName    = fullName,
                            PhoneNumber = phoneNumber,
                            Faculty     = faculties[fIdx],
                            Group       = groups[fIdx][gIdx],
                            BlockNumber = blockRoom,
                        });
                    }
                }
            }
        }

        return rows; // 9 × 3 × 2 × 2 = 108 студентов
    }

    // ─── Выселение — подтверждение менеджером ────────────────────────────────

    public async Task<IActionResult> Eviction()
    {
        ViewData["Title"] = "Выселение";
        var resp = await _api.GetAsync<IEnumerable<EvictionRequestDto>>("api/eviction/pending");
        var vm = new ManagerEvictionViewModel
        {
            PendingEvictions = resp?.Data?.ToList() ?? new(),
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmEviction(List<int> evictionIds)
    {
        if (evictionIds == null || !evictionIds.Any())
        { TempData["Error"] = "Выберите хотя бы одного студента"; return RedirectToAction("Eviction"); }

        var managerId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var resp = await _api.PostAsync<bool>("api/eviction/confirm",
            new ConfirmEvictionDto { ManagerId = managerId, EvictionRequestIds = evictionIds });

        TempData[resp?.Success == true ? "Success" : "Error"] =
            resp?.Success == true ? resp.Message ?? "Выселение выполнено" : resp?.Message ?? "Ошибка";
        return RedirectToAction("Eviction");
    }

    // ─── Вспомогательные методы ──────────────────────────────────────────────

    /// <summary>
    /// Проверяет, что блок указан в формате «NN-1» или «NN-2».
    /// Дефис и явный номер комнаты (1 или 2) обязательны.
    /// </summary>
    private static bool IsRoomNumberValid(string blockNumber)
    {
        var idx = blockNumber.IndexOf('-');
        if (idx <= 0) return false; // дефис обязателен

        var roomPart = blockNumber[(idx + 1)..].Trim();
        return int.TryParse(roomPart, out var room) && (room == 1 || room == 2);
    }
}
