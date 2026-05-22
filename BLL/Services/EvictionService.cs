using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;

namespace BLL.Services;

public class EvictionService : IEvictionService
{
    private readonly IRepository<EvictionRequest> _evictionRepo;
    private readonly IRepository<User>            _userRepo;
    private readonly IRepository<Residence>       _residenceRepo;
    private readonly IRepository<Inspection>      _inspectionRepo;
    private readonly IRepository<RepairRequest>   _repairRequestRepo;
    private readonly IRepository<RepairComment>   _repairCommentRepo;
    private readonly IRepository<StudentPoint>    _pointRepo;
    private readonly IRepository<EventParticipant> _eventParticipantRepo;
    private readonly IRepository<CheckInRequestItem> _checkInItemRepo;

    public EvictionService(
        IRepository<EvictionRequest>    evictionRepo,
        IRepository<User>               userRepo,
        IRepository<Residence>          residenceRepo,
        IRepository<Inspection>         inspectionRepo,
        IRepository<RepairRequest>      repairRequestRepo,
        IRepository<RepairComment>      repairCommentRepo,
        IRepository<StudentPoint>       pointRepo,
        IRepository<EventParticipant>   eventParticipantRepo,
        IRepository<CheckInRequestItem> checkInItemRepo)
    {
        _evictionRepo         = evictionRepo;
        _userRepo             = userRepo;
        _residenceRepo        = residenceRepo;
        _inspectionRepo       = inspectionRepo;
        _repairRequestRepo    = repairRequestRepo;
        _repairCommentRepo    = repairCommentRepo;
        _pointRepo            = pointRepo;
        _eventParticipantRepo = eventParticipantRepo;
        _checkInItemRepo      = checkInItemRepo;
    }

    public async Task<ApiResponse<IEnumerable<EvictionRequestDto>>> CreateRequestsAsync(CreateEvictionRequestDto dto)
    {
        try
        {
            var created = new List<EvictionRequestDto>();
            foreach (var userId in dto.UserIds.Distinct())
            {
                // Не создаём дубликат если уже есть Pending
                var existing = await _evictionRepo.FindAsync(
                    e => e.UserId == userId && e.Status == "Pending");
                if (existing.Any()) continue;

                var request = new EvictionRequest
                {
                    UserId      = userId,
                    EducatorId  = dto.EducatorId,
                    RequestedAt = DateTime.Now,
                    Status      = "Pending",
                };
                await _evictionRepo.AddAsync(request);
            }
            await _evictionRepo.SaveChangesAsync();

            var allPending = await _evictionRepo.FindWithIncludeAsync(
                e => e.Status == "Pending",
                e => e.Student, e => e.Educator);

            return ApiResponse<IEnumerable<EvictionRequestDto>>.Ok(
                allPending.Select(Map),
                $"Создано заявок: {created.Count}");
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<EvictionRequestDto>>.Fail($"Ошибка: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<EvictionRequestDto>>> GetPendingAsync()
    {
        var requests = await _evictionRepo.FindWithIncludeAsync(
            e => e.Status == "Pending",
            e => e.Student, e => e.Educator);

        // Для каждого студента подгружаем текущее проживание
        var userIds = requests.Select(r => r.UserId).ToHashSet();
        var residences = await _residenceRepo.FindWithIncludeAsync(
            r => userIds.Contains(r.UserId) && r.IsCurrent == true,
            r => r.Block, r => r.Room);
        var residenceMap = residences.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var dtos = requests.OrderBy(r => r.RequestedAt).Select(r =>
        {
            var dto = Map(r);
            if (residenceMap.TryGetValue(r.UserId, out var res))
            {
                dto.BlockNumber = res.Block?.BlockNumber;
                dto.RoomNumber  = res.Room?.RoomNumber;
            }
            return dto;
        });

        return ApiResponse<IEnumerable<EvictionRequestDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<bool>> ConfirmBatchAsync(ConfirmEvictionDto dto)
    {
        try
        {
            int confirmed = 0;
            foreach (var requestId in dto.EvictionRequestIds)
            {
                var eviction = await _evictionRepo.GetByIdAsync(requestId);
                if (eviction == null || eviction.Status != "Pending") continue;

                var userId = eviction.UserId;

                // Удаляем все связанные записи
                var residences        = await _residenceRepo.FindAsync(r => r.UserId == userId);
                var points            = await _pointRepo.FindAsync(p => p.UserId == userId);
                var inspections       = await _inspectionRepo.FindAsync(i => i.InspectorId == userId);
                var repairComments    = await _repairCommentRepo.FindAsync(c => c.UserId == userId);
                var repairRequests    = await _repairRequestRepo.FindAsync(r => r.RequestedById == userId);
                var eventParticipants = await _eventParticipantRepo.FindAsync(e => e.UserId == userId);
                var checkInItems      = await _checkInItemRepo.FindAsync(i => i.UserId == userId);

                foreach (var r in residences)        _residenceRepo.Delete(r);
                foreach (var p in points)            _pointRepo.Delete(p);
                foreach (var i in inspections)       _inspectionRepo.Delete(i);
                foreach (var c in repairComments)    _repairCommentRepo.Delete(c);
                foreach (var r in repairRequests)    _repairRequestRepo.Delete(r);
                foreach (var e in eventParticipants) _eventParticipantRepo.Delete(e);
                foreach (var c in checkInItems)
                {
                    c.UserId = null;
                    _checkInItemRepo.Update(c);
                }

                // Удаляем все EvictionRequest, где этот пользователь Student или Educator,
                // иначе non-nullable FK не позволит удалить User
                var allUserEvictions = await _evictionRepo.FindAsync(
                    e => e.UserId == userId || e.EducatorId == userId);
                foreach (var ev in allUserEvictions) _evictionRepo.Delete(ev);

                await _evictionRepo.SaveChangesAsync();

                var user = await _userRepo.GetByIdAsync(userId);
                if (user != null) _userRepo.Delete(user);

                await _userRepo.SaveChangesAsync();
                confirmed++;
            }

            return ApiResponse<bool>.Ok(true, $"Выселено {confirmed} студентов");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Fail($"Ошибка: {ex.Message}");
        }
    }

    private static EvictionRequestDto Map(EvictionRequest r) => new()
    {
        Id              = r.Id,
        UserId          = r.UserId,
        StudentName     = r.Student?.FullName ?? string.Empty,
        StudentUsername = r.Student?.Username ?? string.Empty,
        EducatorId      = r.EducatorId,
        EducatorName    = r.Educator?.FullName ?? string.Empty,
        RequestedAt     = r.RequestedAt,
        Status          = r.Status,
        ManagerId       = r.ManagerId,
        ConfirmedAt     = r.ConfirmedAt,
    };
}
