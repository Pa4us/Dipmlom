using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System.Security.Cryptography;
using System.Text;

namespace BLL.Services;

public class CheckInService : ICheckInService
{
    private readonly IRepository<CheckInRequest>     _requestRepo;
    private readonly IRepository<CheckInRequestItem> _itemRepo;
    private readonly IRepository<User>               _userRepo;
    private readonly IRepository<Role>               _roleRepo;
    private readonly IRepository<Block>              _blockRepo;
    private readonly IRepository<Room>               _roomRepo;
    private readonly IRepository<Residence>          _residenceRepo;
    private readonly IRepository<Faculty>            _facultyRepo;
    private readonly IRepository<Group>              _groupRepo;

    public CheckInService(
        IRepository<CheckInRequest>     requestRepo,
        IRepository<CheckInRequestItem> itemRepo,
        IRepository<User>               userRepo,
        IRepository<Role>               roleRepo,
        IRepository<Block>              blockRepo,
        IRepository<Room>               roomRepo,
        IRepository<Residence>          residenceRepo,
        IRepository<Faculty>            facultyRepo,
        IRepository<Group>              groupRepo)
    {
        _requestRepo   = requestRepo;
        _itemRepo      = itemRepo;
        _userRepo      = userRepo;
        _roleRepo      = roleRepo;
        _blockRepo     = blockRepo;
        _roomRepo      = roomRepo;
        _residenceRepo = residenceRepo;
        _facultyRepo   = facultyRepo;
        _groupRepo     = groupRepo;
    }

    public async Task<ApiResponse<CheckInRequestDto>> CreateRequestAsync(CreateCheckInRequestDto dto)
    {
        try
        {
            var request = new CheckInRequest
            {
                ManagerId = dto.ManagerId,
                CreatedAt = DateTime.Now,
                Status    = "Pending",
            };
            await _requestRepo.AddAsync(request);
            await _requestRepo.SaveChangesAsync();

            foreach (var row in dto.Items)
            {
                var item = new CheckInRequestItem
                {
                    CheckInRequestId = request.Id,
                    FullName         = row.FullName.Trim(),
                    PhoneNumber   = row.PhoneNumber?.Trim(),
                    Faculty          = row.Faculty?.Trim(),
                    Group            = row.Group?.Trim(),
                    BlockNumber      = row.BlockNumber.Trim(),
                    Status           = "Pending",
                };
                await _itemRepo.AddAsync(item);
            }
            await _itemRepo.SaveChangesAsync();

            return ApiResponse<CheckInRequestDto>.Ok(MapRequest(request, dto.Items.Count),
                "Заявка на заселение создана");
        }
        catch (Exception ex)
        {
            return ApiResponse<CheckInRequestDto>.Fail($"Ошибка: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<CheckInRequestDto>>> GetAllRequestsAsync()
    {
        var requests = await _requestRepo.FindWithIncludeAsync(r => true, r => r.Manager, r => r.Items);
        var dtos = requests.OrderByDescending(r => r.CreatedAt)
            .Select(r => MapRequest(r, r.Items.Count, r.Items.Count(i => i.Status == "Pending")));
        return ApiResponse<IEnumerable<CheckInRequestDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<IEnumerable<CheckInRequestItemDto>>> GetPendingItemsAsync()
    {
        var items = await _itemRepo.FindWithIncludeAsync(
            i => i.Status == "Pending",
            i => i.Request);
        var dtos = items.OrderBy(i => i.CheckInRequestId).ThenBy(i => i.FullName)
            .Select(MapItem);
        return ApiResponse<IEnumerable<CheckInRequestItemDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<ProcessCheckInResultDto>> ProcessRequestAsync(int requestId, int educatorId)
    {
        try
        {
            var request = await _requestRepo.GetByIdWithIncludeAsync(requestId, r => r.Items);
            if (request == null)
                return ApiResponse<ProcessCheckInResultDto>.Fail("Заявка не найдена");

            var pendingItems = request.Items.Where(i => i.Status == "Pending").ToList();
            if (!pendingItems.Any())
                return ApiResponse<ProcessCheckInResultDto>.Fail("Нет студентов для заселения");

            var roles = await _roleRepo.FindAsync(r => r.Name == "Student");
            var studentRole = roles.FirstOrDefault();
            if (studentRole == null)
                return ApiResponse<ProcessCheckInResultDto>.Fail("Роль Student не найдена в системе");

            var result = new ProcessCheckInResultDto();

            foreach (var item in pendingItems)
            {
                // Поиск блока и комнаты
                var (blockNum, roomNum) = ParseBlockNumber(item.BlockNumber);
                var blocks = await _blockRepo.FindAsync(b => b.BlockNumber == blockNum);
                var block  = blocks.FirstOrDefault();
                if (block == null) continue;

                Room? room = null;
                if (!string.IsNullOrEmpty(roomNum))
                {
                    var rooms = await _roomRepo.FindAsync(r => r.BlockId == block.Id && r.RoomNumber == roomNum);
                    room = rooms.FirstOrDefault();
                }
                room ??= (await _roomRepo.FindAsync(r => r.BlockId == block.Id && r.IsActive == true)).FirstOrDefault();
                if (room == null) continue;

                // Faculty / Group — find or create
                int? facultyId = await GetOrCreateFacultyAsync(item.Faculty);
                int? groupId   = await GetOrCreateGroupAsync(item.Group, facultyId);

                // Генерация логина и пароля
                var username = await GenerateUsernameAsync(item.FullName);
                var password = GeneratePassword();
                var hash     = HashPassword(password);

                var user = new User
                {
                    FullName       = item.FullName,
                    Username       = username,
                    Email          = $"{username}@ggtu.by",
                    PasswordHash   = hash,
                    RoleId         = studentRole.Id,
                    FacultyId      = facultyId,
                    GroupId        = groupId,
                    PhoneNumber = item.PhoneNumber,
                    IsActive       = true,
                    CreatedAt      = DateTime.Now,
                };
                await _userRepo.AddAsync(user);
                await _userRepo.SaveChangesAsync();

                // Создаём запись о проживании
                var residence = new Residence
                {
                    UserId     = user.Id,
                    BlockId    = block.Id,
                    RoomId     = room.Id,
                    MoveInDate = DateOnly.FromDateTime(DateTime.Today),
                    IsCurrent  = true,
                };
                await _residenceRepo.AddAsync(residence);

                // Обновляем элемент заявки
                item.Status            = "CheckedIn";
                item.UserId            = user.Id;
                item.GeneratedPassword = password;
                _itemRepo.Update(item);

                result.CreatedItems.Add(new CheckInRequestItemDto
                {
                    Id               = item.Id,
                    CheckInRequestId = item.CheckInRequestId,
                    FullName         = item.FullName,
                    PhoneNumber   = item.PhoneNumber,
                    Faculty          = item.Faculty,
                    Group            = item.Group,
                    BlockNumber      = item.BlockNumber,
                    Status           = "CheckedIn",
                    UserId           = user.Id,
                    Username         = username,
                    GeneratedPassword = password,
                });
                result.ProcessedCount++;
            }

            await _residenceRepo.SaveChangesAsync();
            await _itemRepo.SaveChangesAsync();

            // Обновляем статус заявки, если все позиции обработаны
            var allItems = await _itemRepo.FindAsync(i => i.CheckInRequestId == requestId);
            if (allItems.All(i => i.Status != "Pending"))
            {
                request.Status = "Completed";
                _requestRepo.Update(request);
                await _requestRepo.SaveChangesAsync();
            }

            return ApiResponse<ProcessCheckInResultDto>.Ok(result,
                $"Заселено {result.ProcessedCount} студентов");
        }
        catch (Exception ex)
        {
            return ApiResponse<ProcessCheckInResultDto>.Fail($"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Обрабатывает только выбранные позиции заявки (по их ID).
    /// Используется при мультивыборе воспитателем.
    /// </summary>
    public async Task<ApiResponse<ProcessCheckInResultDto>> ProcessItemsAsync(List<int> itemIds, int educatorId)
    {
        try
        {
            if (itemIds == null || itemIds.Count == 0)
                return ApiResponse<ProcessCheckInResultDto>.Fail("Не выбраны студенты для заселения");

            var roles = await _roleRepo.FindAsync(r => r.Name == "Student");
            var studentRole = roles.FirstOrDefault();
            if (studentRole == null)
                return ApiResponse<ProcessCheckInResultDto>.Fail("Роль Student не найдена в системе");

            var result = new ProcessCheckInResultDto();

            foreach (var itemId in itemIds)
            {
                var items = await _itemRepo.FindAsync(i => i.Id == itemId && i.Status == "Pending");
                var item  = items.FirstOrDefault();
                if (item == null) continue;

                // Поиск блока и комнаты
                var (blockNum, roomNum) = ParseBlockNumber(item.BlockNumber);
                var blocks = await _blockRepo.FindAsync(b => b.BlockNumber == blockNum);
                var block  = blocks.FirstOrDefault();
                if (block == null) continue;

                Room? room = null;
                if (!string.IsNullOrEmpty(roomNum))
                {
                    var rooms = await _roomRepo.FindAsync(r => r.BlockId == block.Id && r.RoomNumber == roomNum);
                    room = rooms.FirstOrDefault();
                }
                room ??= (await _roomRepo.FindAsync(r => r.BlockId == block.Id && r.IsActive == true)).FirstOrDefault();
                if (room == null) continue;

                // Faculty / Group — find or create
                int? facultyId = await GetOrCreateFacultyAsync(item.Faculty);
                int? groupId   = await GetOrCreateGroupAsync(item.Group, facultyId);

                // Генерация логина и пароля
                var username = await GenerateUsernameAsync(item.FullName);
                var password = GeneratePassword();
                var hash     = HashPassword(password);

                var user = new User
                {
                    FullName       = item.FullName,
                    Username       = username,
                    Email          = $"{username}@ggtu.by",
                    PasswordHash   = hash,
                    RoleId         = studentRole.Id,
                    FacultyId      = facultyId,
                    GroupId        = groupId,
                    PhoneNumber = item.PhoneNumber,
                    IsActive       = true,
                    CreatedAt      = DateTime.Now,
                };
                await _userRepo.AddAsync(user);
                await _userRepo.SaveChangesAsync();

                // Запись о проживании
                var residence = new Residence
                {
                    UserId     = user.Id,
                    BlockId    = block.Id,
                    RoomId     = room.Id,
                    MoveInDate = DateOnly.FromDateTime(DateTime.Today),
                    IsCurrent  = true,
                };
                await _residenceRepo.AddAsync(residence);

                // Обновляем элемент заявки
                item.Status            = "CheckedIn";
                item.UserId            = user.Id;
                item.GeneratedPassword = password;
                _itemRepo.Update(item);

                result.CreatedItems.Add(new CheckInRequestItemDto
                {
                    Id                = item.Id,
                    CheckInRequestId  = item.CheckInRequestId,
                    FullName          = item.FullName,
                    PhoneNumber    = item.PhoneNumber,
                    Faculty           = item.Faculty,
                    Group             = item.Group,
                    BlockNumber       = item.BlockNumber,
                    Status            = "CheckedIn",
                    UserId            = user.Id,
                    Username          = username,
                    GeneratedPassword = password,
                });
                result.ProcessedCount++;
            }

            await _residenceRepo.SaveChangesAsync();
            await _itemRepo.SaveChangesAsync();

            // Обновляем статус заявок, все позиции которых теперь обработаны
            var affectedRequestIds = result.CreatedItems.Select(i => i.CheckInRequestId).Distinct().ToList();
            foreach (var reqId in affectedRequestIds)
            {
                var allItems = await _itemRepo.FindAsync(i => i.CheckInRequestId == reqId);
                if (allItems.All(i => i.Status != "Pending"))
                {
                    var reqs = await _requestRepo.FindAsync(r => r.Id == reqId);
                    var req  = reqs.FirstOrDefault();
                    if (req != null)
                    {
                        req.Status = "Completed";
                        _requestRepo.Update(req);
                    }
                }
            }
            await _requestRepo.SaveChangesAsync();

            return ApiResponse<ProcessCheckInResultDto>.Ok(result,
                $"Заселено {result.ProcessedCount} студентов");
        }
        catch (Exception ex)
        {
            return ApiResponse<ProcessCheckInResultDto>.Fail($"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Отклоняет выбранные позиции заявки (статус → Rejected).
    /// Если все позиции заявки больше не Pending — помечает саму заявку как Completed.
    /// </summary>
    public async Task<ApiResponse<int>> RejectItemsAsync(List<int> itemIds)
    {
        try
        {
            if (itemIds == null || itemIds.Count == 0)
                return ApiResponse<int>.Fail("Не выбраны студенты для отклонения");

            int rejectedCount = 0;
            var affectedRequestIds = new HashSet<int>();

            foreach (var itemId in itemIds)
            {
                var items = await _itemRepo.FindAsync(i => i.Id == itemId && i.Status == "Pending");
                var item  = items.FirstOrDefault();
                if (item == null) continue;

                item.Status = "Rejected";
                _itemRepo.Update(item);
                affectedRequestIds.Add(item.CheckInRequestId);
                rejectedCount++;
            }

            await _itemRepo.SaveChangesAsync();

            // Закрываем заявки, у которых не осталось Pending-позиций
            foreach (var reqId in affectedRequestIds)
            {
                var allItems = await _itemRepo.FindAsync(i => i.CheckInRequestId == reqId);
                if (allItems.All(i => i.Status != "Pending"))
                {
                    var reqs = await _requestRepo.FindAsync(r => r.Id == reqId);
                    var req  = reqs.FirstOrDefault();
                    if (req != null)
                    {
                        req.Status = "Completed";
                        _requestRepo.Update(req);
                    }
                }
            }
            await _requestRepo.SaveChangesAsync();

            return ApiResponse<int>.Ok(rejectedCount, $"Отклонено {rejectedCount} студентов");
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.Fail($"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Возвращает все позиции со статусом CheckedIn,
    /// у которых связанный аккаунт создан не позже <paramref name="days"/> дней назад.
    /// Используется для сводного PDF с паролями за период.
    /// </summary>
    public async Task<ApiResponse<IEnumerable<CheckInRequestItemDto>>> GetRecentlyCheckedInAsync(int days)
    {
        var cutoff = DateTime.Now.AddDays(-days);

        // Загружаем позиции, заселённые (есть UserId), вместе с пользователем
        var items = await _itemRepo.FindWithIncludeAsync(
            i => i.Status == "CheckedIn" && i.UserId != null,
            i => i.User);

        // Фильтруем по дате создания аккаунта
        var recent = items
            .Where(i => i.User != null && i.User.CreatedAt >= cutoff)
            .OrderByDescending(i => i.User!.CreatedAt)
            .Select(i => new CheckInRequestItemDto
            {
                Id                = i.Id,
                CheckInRequestId  = i.CheckInRequestId,
                FullName          = i.FullName,
                PhoneNumber    = i.PhoneNumber,
                Faculty           = i.Faculty,
                Group             = i.Group,
                BlockNumber       = i.BlockNumber,
                Status            = i.Status,
                UserId            = i.UserId,
                Username          = i.User?.Username,
                GeneratedPassword = i.GeneratedPassword,
            });

        return ApiResponse<IEnumerable<CheckInRequestItemDto>>.Ok(recent);
    }

    // ─── Вспомогательные методы ──────────────────────────────────────────────

    private static (string block, string room) ParseBlockNumber(string blockNumber)
    {
        var idx = blockNumber.IndexOf('-');
        return idx > 0
            ? (blockNumber[..idx].Trim(), blockNumber[(idx + 1)..].Trim())
            : (blockNumber.Trim(), string.Empty);
    }

    private async Task<int?> GetOrCreateFacultyAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var existing = await _facultyRepo.FindAsync(f => f.Name == name.Trim());
        if (existing.FirstOrDefault() is { } f) return f.Id;
        var entity = new Faculty { Name = name.Trim() };
        await _facultyRepo.AddAsync(entity);
        await _facultyRepo.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<int?> GetOrCreateGroupAsync(string? name, int? facultyId)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var existing = await _groupRepo.FindAsync(g => g.Name == name.Trim() && g.FacultyId == facultyId);
        if (existing.FirstOrDefault() is { } g) return g.Id;
        var entity = new Group { Name = name.Trim(), FacultyId = facultyId };
        await _groupRepo.AddAsync(entity);
        await _groupRepo.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<string> GenerateUsernameAsync(string fullName)
    {
        // Транслитерация: берём первые буквы
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lastName  = Transliterate(parts.Length > 0 ? parts[0] : "user").ToLower();
        var initials  = string.Concat(parts.Skip(1).Select(p => Transliterate(p.Substring(0, 1)).ToLower()));
        var candidate = lastName + initials;
        if (candidate.Length > 20) candidate = candidate[..20];

        var existing = await _userRepo.FindAsync(u => u.Username.StartsWith(candidate));
        if (!existing.Any()) return candidate;

        // Добавляем числовой суффикс
        for (int i = 1; i <= 999; i++)
        {
            var withSuffix = candidate + i;
            if (existing.All(u => u.Username != withSuffix))
                return withSuffix;
        }
        return candidate + Guid.NewGuid().ToString("N")[..4];
    }

    private static string Transliterate(string s)
    {
        var map = new Dictionary<char, string>
        {
            ['а']="a",['б']="b",['в']="v",['г']="g",['д']="d",['е']="e",['ё']="yo",
            ['ж']="zh",['з']="z",['и']="i",['й']="y",['к']="k",['л']="l",['м']="m",
            ['н']="n",['о']="o",['п']="p",['р']="r",['с']="s",['т']="t",['у']="u",
            ['ф']="f",['х']="kh",['ц']="ts",['ч']="ch",['ш']="sh",['щ']="sch",
            ['ъ']="",['ы']="y",['ь']="",['э']="e",['ю']="yu",['я']="ya",
        };
        var sb = new StringBuilder();
        foreach (var c in s.ToLower())
            sb.Append(map.TryGetValue(c, out var t) ? t : c.ToString());
        return sb.ToString();
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static string GeneratePassword()
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static CheckInRequestDto MapRequest(CheckInRequest r, int total, int pending = 0) => new()
    {
        Id           = r.Id,
        ManagerId    = r.ManagerId,
        ManagerName  = r.Manager?.FullName ?? string.Empty,
        CreatedAt    = r.CreatedAt,
        Status       = r.Status,
        TotalCount   = total,
        PendingCount = pending,
    };

    private static CheckInRequestItemDto MapItem(CheckInRequestItem i) => new()
    {
        Id               = i.Id,
        CheckInRequestId = i.CheckInRequestId,
        FullName         = i.FullName,
        PhoneNumber   = i.PhoneNumber,
        Faculty          = i.Faculty,
        Group            = i.Group,
        BlockNumber      = i.BlockNumber,
        Status           = i.Status,
        UserId           = i.UserId,
        Username         = i.User?.Username,
        GeneratedPassword = i.GeneratedPassword,
    };
}
