using AutoMapper;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BLL.Services
{
    public class ResidenceService : BaseService<Residence, ResidenceDto, CreateResidenceDto, UpdateResidenceDto>, IResidenceService
    {
        private readonly IRoomService      _roomService;
        private readonly IRepository<User>  _userRepository;
        private readonly IRepository<Block> _blockRepository;
        private readonly IRepository<Room>  _roomRepository;

        public ResidenceService(
            IRepository<Residence> repository,
            IRoomService           roomService,
            IRepository<User>      userRepository,
            IRepository<Block>     blockRepository,
            IRepository<Room>      roomRepository,
            IMapper                mapper)
            : base(repository, mapper)
        {
            _roomService     = roomService;
            _userRepository  = userRepository;
            _blockRepository = blockRepository;
            _roomRepository  = roomRepository;
        }

        private static readonly System.Linq.Expressions.Expression<Func<Residence, object>>[] _includes =
        {
            r => r.User,
            r => r.Room,
            r => r.Block
        };

        public override async Task<ApiResponse<IEnumerable<ResidenceDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(_includes);
            return ApiResponse<IEnumerable<ResidenceDto>>.Ok(_mapper.Map<IEnumerable<ResidenceDto>>(entities));
        }

        public override async Task<ApiResponse<ResidenceDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, _includes);
            if (entity == null) return ApiResponse<ResidenceDto>.Fail($"Проживание с ID {id} не найдено");
            return ApiResponse<ResidenceDto>.Ok(_mapper.Map<ResidenceDto>(entity));
        }

        public async Task<ApiResponse<ResidenceDto>> GetCurrentResidenceByUserAsync(int userId)
        {
            var residences = await _repository.FindWithIncludeAsync(r => r.UserId == userId && r.IsCurrent == true, _includes);
            var residence = residences.FirstOrDefault();

            if (residence == null)
                return ApiResponse<ResidenceDto>.Fail($"Активное проживание для пользователя {userId} не найдено");

            return ApiResponse<ResidenceDto>.Ok(_mapper.Map<ResidenceDto>(residence));
        }

        public async Task<ApiResponse<IEnumerable<ResidenceDto>>> GetResidenceHistoryByUserAsync(int userId)
        {
            var residences = await _repository.FindWithIncludeAsync(r => r.UserId == userId, _includes);
            return ApiResponse<IEnumerable<ResidenceDto>>.Ok(_mapper.Map<IEnumerable<ResidenceDto>>(residences));
        }

        public async Task<ApiResponse<IEnumerable<ResidenceDto>>> GetCurrentResidentsByRoomAsync(int roomId)
        {
            var residences = await _repository.FindWithIncludeAsync(r => r.RoomId == roomId && r.IsCurrent == true, _includes);
            return ApiResponse<IEnumerable<ResidenceDto>>.Ok(_mapper.Map<IEnumerable<ResidenceDto>>(residences));
        }

        public override async Task<ApiResponse<ResidenceDto>> CreateAsync(CreateResidenceDto createDto)
        {
            // Проверка, что комната не переполнена
            var roomResponse = await _roomService.GetByIdAsync(createDto.RoomId);
            if (!roomResponse.Success || roomResponse.Data == null)
                return ApiResponse<ResidenceDto>.Fail("Комната не найдена");

            if (roomResponse.Data.CurrentOccupancy >= roomResponse.Data.Capacity)
                return ApiResponse<ResidenceDto>.Fail("В комнате нет свободных мест");

            // Проверка, что студент еще не заселен
            var currentResidence = await GetCurrentResidenceByUserAsync(createDto.UserId);
            if (currentResidence.Success && currentResidence.Data != null)
                return ApiResponse<ResidenceDto>.Fail("Студент уже заселен в другую комнату");

            // Заселение — BlockId берём из комнаты (в DTO его нет)
            var entity = _mapper.Map<Residence>(createDto);
            entity.BlockId = roomResponse.Data.BlockId;
            entity.IsCurrent = true;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            await _roomService.UpdateOccupancyAsync(createDto.RoomId, 1);

            var dto = _mapper.Map<ResidenceDto>(entity);
            return ApiResponse<ResidenceDto>.Ok(dto, "Студент успешно заселён");
        }

        public async Task<ApiResponse<ResidenceDto>> CheckOutAsync(int residenceId, DateOnly moveOutDate)
        {
            var residence = await _repository.GetByIdAsync(residenceId);
            if (residence == null)
                return ApiResponse<ResidenceDto>.Fail($"Проживание с ID {residenceId} не найдено");

            residence.MoveOutDate = moveOutDate;
            residence.IsCurrent = false;

            _repository.Update(residence);
            await _repository.SaveChangesAsync();

            await _roomService.UpdateOccupancyAsync(residence.RoomId, -1);

            var dto = _mapper.Map<ResidenceDto>(residence);
            return ApiResponse<ResidenceDto>.Ok(dto, "Студент выселен");
        }

        public async Task<ApiResponse<ImportResidencesResultDto>> ImportResidencesAsync(ImportResidencesRequestDto dto)
        {
            // Загружаем справочники одним пакетом
            var allUsers    = await _userRepository.GetAllAsync();
            var allBlocks   = await _blockRepository.GetAllAsync();
            var allRooms    = await _roomRepository.FindWithIncludeAsync(r => r.IsActive != false, r => r.Block);
            var currentRes  = await _repository.FindAsync(r => r.IsCurrent == true);

            var usernameMap    = allUsers.ToDictionary(u => u.Username.ToLower(), u => u);
            var blockMap       = allBlocks.ToDictionary(b => b.BlockNumber.ToLower(), b => b);
            var alreadySeated  = currentRes.Select(r => r.UserId).ToHashSet();

            // Комнаты с местами: BlockId → список комнат
            var freeRoomsByBlock = allRooms
                .Where(r => r.CurrentOccupancy < r.Capacity)
                .GroupBy(r => r.BlockId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RoomNumber).ToList());

            var result = new ImportResidencesResultDto();
            // Отслеживаем студентов, уже назначенных в рамках этого файла
            var seatedInBatch = new HashSet<int>();
            // Комнаты, занятые в рамках этого батча: RoomId → кол-во занятых мест
            var roomOccupancyDelta = new Dictionary<int, int>();

            foreach (var row in dto.Rows)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(row.Username))  { errors.Add("Не указан логин"); }
                if (string.IsNullOrWhiteSpace(row.BlockNumber)){ errors.Add("Не указан блок");  }

                if (errors.Any()) { row.Error = string.Join("; ", errors); result.InvalidRows.Add(row); continue; }

                // Ищем пользователя
                if (!usernameMap.TryGetValue(row.Username.Trim().ToLower(), out var user))
                { row.Error = $"Пользователь «{row.Username}» не найден"; result.InvalidRows.Add(row); continue; }

                if (alreadySeated.Contains(user.Id) || seatedInBatch.Contains(user.Id))
                { row.Error = "Студент уже заселён"; result.InvalidRows.Add(row); continue; }

                // Ищем блок
                if (!blockMap.TryGetValue(row.BlockNumber.Trim().ToLower(), out var block))
                { row.Error = $"Блок «{row.BlockNumber}» не найден"; result.InvalidRows.Add(row); continue; }

                // Ищем комнату
                Room? room = null;
                if (!string.IsNullOrWhiteSpace(row.RoomNumber))
                {
                    room = allRooms.FirstOrDefault(r =>
                        r.BlockId == block.Id &&
                        r.RoomNumber.Equals(row.RoomNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (room == null)
                    { row.Error = $"Комната «{row.RoomNumber}» в блоке «{row.BlockNumber}» не найдена"; result.InvalidRows.Add(row); continue; }

                    var delta = roomOccupancyDelta.GetValueOrDefault(room.Id, 0);
                    if (room.CurrentOccupancy + delta >= room.Capacity)
                    { row.Error = $"Комната «{row.RoomNumber}» заполнена"; result.InvalidRows.Add(row); continue; }
                }
                else
                {
                    // Первая свободная комната в блоке (с учётом батча)
                    if (freeRoomsByBlock.TryGetValue(block.Id, out var freeList))
                    {
                        room = freeList.FirstOrDefault(r =>
                            r.CurrentOccupancy + roomOccupancyDelta.GetValueOrDefault(r.Id, 0) < r.Capacity);
                    }
                    if (room == null)
                    { row.Error = $"В блоке «{row.BlockNumber}» нет свободных комнат"; result.InvalidRows.Add(row); continue; }
                }

                // Всё валидно
                row.AssignedRoom = room.RoomNumber;
                seatedInBatch.Add(user.Id);
                roomOccupancyDelta[room.Id] = roomOccupancyDelta.GetValueOrDefault(room.Id, 0) + 1;
                result.ValidRows.Add(row);

                if (!dto.DryRun)
                {
                    var residence = new Residence
                    {
                        UserId     = user.Id,
                        RoomId     = room.Id,
                        BlockId    = block.Id,
                        MoveInDate = DateOnly.FromDateTime(DateTime.Today),
                        IsCurrent  = true,
                    };
                    await _repository.AddAsync(residence);
                    await _roomService.UpdateOccupancyAsync(room.Id, 1);
                    result.CreatedCount++;
                }
            }

            if (!dto.DryRun && result.CreatedCount > 0)
                await _repository.SaveChangesAsync();

            return ApiResponse<ImportResidencesResultDto>.Ok(result,
                dto.DryRun
                    ? $"Проверка: {result.ValidRows.Count} валидных, {result.InvalidRows.Count} с ошибками"
                    : $"Заселено {result.CreatedCount} студентов");
        }
    }
}
