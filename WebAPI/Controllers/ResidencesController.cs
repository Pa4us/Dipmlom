using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;

namespace WebAPI.Controllers
{
    [Authorize]
    public class ResidencesController : BaseApiController
    {
        private readonly IResidenceService _residenceService;

        public ResidencesController(IResidenceService residenceService)
        {
            _residenceService = residenceService;
        }

        /// <summary>Получить все записи о проживании</summary>
        [HttpGet]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _residenceService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить запись по ID</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _residenceService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить текущее проживание студента</summary>
        [HttpGet("current/user/{userId:int}")]
        public async Task<IActionResult> GetCurrentByUser(int userId)
        {
            var response = await _residenceService.GetCurrentResidenceByUserAsync(userId);
            return HandleResponseNotFound(response);
        }

        /// <summary>История проживания студента</summary>
        [HttpGet("history/user/{userId:int}")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> GetHistoryByUser(int userId)
        {
            var response = await _residenceService.GetResidenceHistoryByUserAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Текущие жильцы комнаты</summary>
        [HttpGet("current/room/{roomId:int}")]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> GetCurrentByRoom(int roomId)
        {
            var response = await _residenceService.GetCurrentResidentsByRoomAsync(roomId);
            return HandleResponse(response);
        }

        /// <summary>Заселить студента</summary>
        [HttpPost]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> CheckIn([FromBody] CreateResidenceDto dto)
        {
            var response = await _residenceService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Выселить студента</summary>
        [HttpPost("{id:int}/checkout")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> CheckOut(int id, [FromBody] CheckOutDto dto)
        {
            var response = await _residenceService.CheckOutAsync(id, dto.MoveOutDate);
            return HandleResponse(response);
        }

        /// <summary>Массовое заселение из Excel (превью + применение)</summary>
        [HttpPost("import")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> ImportResidences([FromBody] ImportResidencesRequestDto dto)
        {
            var response = await _residenceService.ImportResidencesAsync(dto);
            return HandleResponse(response);
        }
    }

    public class CheckOutDto
    {
        public DateOnly MoveOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    }
}
