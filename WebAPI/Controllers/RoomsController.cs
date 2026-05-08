using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers
{
    [Authorize]
    public class RoomsController : BaseApiController
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        /// <summary>Получить все комнаты</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _roomService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить комнату по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _roomService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить комнаты блока</summary>
        [HttpGet("by-block/{blockId:int}")]
        public async Task<IActionResult> GetByBlock(int blockId)
        {
            var response = await _roomService.GetRoomsByBlockAsync(blockId);
            return HandleResponse(response);
        }

        /// <summary>Получить свободные комнаты</summary>
        [HttpGet("free")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> GetFree()
        {
            var response = await _roomService.GetFreeRoomsAsync();
            return HandleResponse(response);
        }

        /// <summary>Создать комнату</summary>
        [HttpPost]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            var response = await _roomService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить комнату</summary>
        [HttpPut]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Update([FromBody] UpdateRoomDto dto)
        {
            var response = await _roomService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Удалить комнату</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _roomService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
