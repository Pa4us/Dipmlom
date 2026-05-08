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
    public class BlockService : BaseService<Block, BlockDto, CreateBlockDto, UpdateBlockDto>, IBlockService
    {
        public BlockService(IRepository<Block> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }

        public override async Task<ApiResponse<IEnumerable<BlockDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(b => b.Rooms);
            return ApiResponse<IEnumerable<BlockDto>>.Ok(_mapper.Map<IEnumerable<BlockDto>>(entities));
        }

        public override async Task<ApiResponse<BlockDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, b => b.Rooms);
            if (entity == null) return ApiResponse<BlockDto>.Fail($"Блок с ID {id} не найден");
            return ApiResponse<BlockDto>.Ok(_mapper.Map<BlockDto>(entity));
        }

        public async Task<ApiResponse<BlockDto>> GetBlockWithRoomsAsync(int id)
        {
            var block = await _repository.GetByIdWithIncludeAsync(id, b => b.Rooms);
            if (block == null)
                return ApiResponse<BlockDto>.Fail($"Блок с ID {id} не найден");

            var dto = _mapper.Map<BlockDto>(block);
            return ApiResponse<BlockDto>.Ok(dto);
        }

        public async Task<ApiResponse<IEnumerable<BlockDto>>> GetBlocksByFloorAsync(int floor)
        {
            var blocks = await _repository.FindWithIncludeAsync(b => b.Floor == floor, b => b.Rooms);
            return ApiResponse<IEnumerable<BlockDto>>.Ok(_mapper.Map<IEnumerable<BlockDto>>(blocks));
        }

        public async Task<ApiResponse<IEnumerable<BlockDto>>> GetBlocksByRangeAsync(int floorFrom, int floorTo)
        {
            var blocks = await _repository.FindWithIncludeAsync(b => b.Floor >= floorFrom && b.Floor <= floorTo, b => b.Rooms);
            return ApiResponse<IEnumerable<BlockDto>>.Ok(_mapper.Map<IEnumerable<BlockDto>>(blocks));
        }
    }
}
