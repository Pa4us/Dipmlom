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

namespace BLL.Services
{
    public class ResidenceService : BaseService<Residence, ResidenceDto, CreateResidenceDto, UpdateResidenceDto>, IResidenceService
    {
        private readonly IRoomService _roomService;

        public ResidenceService(IRepository<Residence> repository, IRoomService roomService, IMapper mapper)
            : base(repository, mapper)
        {
            _roomService = roomService;
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
    }
}
