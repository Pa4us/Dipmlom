using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers
{
    [Authorize]
    public class BlocksController : BaseApiController
    {
        private readonly IBlockService _blockService;

        public BlocksController(IBlockService blockService)
        {
            _blockService = blockService;
        }

        /// <summary>Получить все блоки</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _blockService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить блок по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _blockService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить блок с комнатами</summary>
        [HttpGet("{id:int}/rooms")]
        public async Task<IActionResult> GetWithRooms(int id)
        {
            var response = await _blockService.GetBlockWithRoomsAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить блоки по этажу</summary>
        [HttpGet("by-floor/{floor:int}")]
        public async Task<IActionResult> GetByFloor(int floor)
        {
            var response = await _blockService.GetBlocksByFloorAsync(floor);
            return HandleResponse(response);
        }

        /// <summary>Получить блоки в диапазоне этажей</summary>
        [HttpGet("by-floor-range")]
        public async Task<IActionResult> GetByFloorRange([FromQuery] int from, [FromQuery] int to)
        {
            var response = await _blockService.GetBlocksByRangeAsync(from, to);
            return HandleResponse(response);
        }

        /// <summary>Создать блок</summary>
        [HttpPost]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateBlockDto dto)
        {
            var response = await _blockService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить блок</summary>
        [HttpPut]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Update([FromBody] UpdateBlockDto dto)
        {
            var response = await _blockService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Удалить блок</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _blockService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
