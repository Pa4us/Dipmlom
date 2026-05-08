using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Authorize]
    public class RepairRequestsController : BaseApiController
    {
        private readonly IRepairRequestService _repairRequestService;

        public RepairRequestsController(IRepairRequestService repairRequestService)
        {
            _repairRequestService = repairRequestService;
        }

        /// <summary>Получить все заявки</summary>
        [HttpGet]
        [Authorize(Roles = "Educator,Mechanic,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _repairRequestService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить заявку по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _repairRequestService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Мои заявки (текущий пользователь)</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _repairRequestService.GetRequestsByUserAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Заявки назначенные мне (слесарь)</summary>
        [HttpGet("assigned-to-me")]
        [Authorize(Roles = "Mechanic")]
        public async Task<IActionResult> GetAssignedToMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _repairRequestService.GetRequestsAssignedToMeAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Заявки по статусу</summary>
        [HttpGet("by-status/{status}")]
        [Authorize(Roles = "Educator,Mechanic")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var response = await _repairRequestService.GetRequestsByStatusAsync(status);
            return HandleResponse(response);
        }

        /// <summary>Заявки по блоку</summary>
        [HttpGet("by-block/{blockId:int}")]
        [Authorize(Roles = "Educator,Mechanic")]
        public async Task<IActionResult> GetByBlock(int blockId)
        {
            var response = await _repairRequestService.GetRequestsByBlockAsync(blockId);
            return HandleResponse(response);
        }

        /// <summary>Создать заявку на ремонт</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRepairRequestDto dto)
        {
            dto.RequestedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _repairRequestService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить заявку</summary>
        [HttpPut]
        [Authorize(Roles = "Educator,Mechanic")]
        public async Task<IActionResult> Update([FromBody] UpdateRepairRequestDto dto)
        {
            var response = await _repairRequestService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Изменить статус заявки</summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Educator,Mechanic")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRepairRequestStatusDto dto)
        {
            var response = await _repairRequestService.UpdateStatusAsync(id, dto.Status, dto.Comment);
            return HandleResponse(response);
        }

        /// <summary>Назначить слесаря</summary>
        [HttpPatch("{id:int}/assign")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignRepairRequestDto dto)
        {
            var response = await _repairRequestService.AssignToMechanicAsync(id, dto.MechanicId);
            return HandleResponse(response);
        }

        /// <summary>Добавить комментарий к заявке</summary>
        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _repairRequestService.AddCommentAsync(id, userId, dto.Comment);
            return HandleResponse(response);
        }

        /// <summary>Получить комментарии заявки</summary>
        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var response = await _repairRequestService.GetCommentsAsync(id);
            return HandleResponse(response);
        }

        /// <summary>Удалить заявку</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _repairRequestService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
