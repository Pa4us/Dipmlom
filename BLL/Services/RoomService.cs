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
    public class RoomService : BaseService<Room, RoomDto, CreateRoomDto, UpdateRoomDto>, IRoomService
    {
        public RoomService(IRepository<Room> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }

        public override async Task<ApiResponse<IEnumerable<RoomDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(r => r.Block);
            return ApiResponse<IEnumerable<RoomDto>>.Ok(_mapper.Map<IEnumerable<RoomDto>>(entities));
        }

        public override async Task<ApiResponse<RoomDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, r => r.Block);
            if (entity == null) return ApiResponse<RoomDto>.Fail($"Комната с ID {id} не найдена");
            return ApiResponse<RoomDto>.Ok(_mapper.Map<RoomDto>(entity));
        }

        public async Task<ApiResponse<IEnumerable<RoomDto>>> GetRoomsByBlockAsync(int blockId)
        {
            var rooms = await _repository.FindWithIncludeAsync(r => r.BlockId == blockId, r => r.Block);
            return ApiResponse<IEnumerable<RoomDto>>.Ok(_mapper.Map<IEnumerable<RoomDto>>(rooms));
        }

        public async Task<ApiResponse<IEnumerable<RoomDto>>> GetFreeRoomsAsync()
        {
            var rooms = await _repository.FindWithIncludeAsync(r => r.CurrentOccupancy < r.Capacity && r.IsActive == true, r => r.Block);
            return ApiResponse<IEnumerable<RoomDto>>.Ok(_mapper.Map<IEnumerable<RoomDto>>(rooms));
        }

        public async Task<ApiResponse<bool>> UpdateOccupancyAsync(int roomId, int delta)
        {
            var room = await _repository.GetByIdAsync(roomId);
            if (room == null)
                return ApiResponse<bool>.Fail($"Комната с ID {roomId} не найдена");

            var newOccupancy = room.CurrentOccupancy + delta;
            if (newOccupancy < 0 || newOccupancy > room.Capacity)
                return ApiResponse<bool>.Fail($"Некорректное изменение заполняемости. Текущая: {room.CurrentOccupancy}, попытка изменить на {delta}");

            room.CurrentOccupancy = newOccupancy;
            _repository.Update(room);
            await _repository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true);
        }
    }
}
