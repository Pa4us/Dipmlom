using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Authorize]
    public class EventsController : BaseApiController
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>Все мероприятия</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _eventService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить мероприятие по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _eventService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Предстоящие мероприятия</summary>
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming()
        {
            var response = await _eventService.GetUpcomingEventsAsync();
            return HandleResponse(response);
        }

        /// <summary>Мероприятия за период</summary>
        [HttpGet("by-date")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var response = await _eventService.GetEventsByDateRangeAsync(from, to);
            return HandleResponse(response);
        }

        /// <summary>Участники мероприятия</summary>
        [HttpGet("{id:int}/participants")]
        public async Task<IActionResult> GetParticipants(int id)
        {
            var response = await _eventService.GetParticipantsAsync(id);
            return HandleResponse(response);
        }

        /// <summary>Проверить, зарегистрирован ли студент</summary>
        [HttpGet("{id:int}/is-registered/{studentId:int}")]
        public async Task<IActionResult> IsRegistered(int id, int studentId)
        {
            var response = await _eventService.CheckStudentRegisteredAsync(id, studentId);
            return HandleResponse(response);
        }

        /// <summary>Создать мероприятие</summary>
        [HttpPost]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            dto.OrganizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _eventService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить мероприятие</summary>
        [HttpPut]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> Update([FromBody] UpdateEventDto dto)
        {
            var response = await _eventService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Зарегистрировать текущего студента на мероприятие</summary>
        [HttpPost("{id:int}/register")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Register(int id)
        {
            var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _eventService.RegisterStudentAsync(id, studentId);
            return HandleResponse(response);
        }

        /// <summary>Зарегистрировать конкретного студента (для воспитателя)</summary>
        [HttpPost("{id:int}/register/{studentId:int}")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> RegisterStudent(int id, int studentId)
        {
            var response = await _eventService.RegisterStudentAsync(id, studentId);
            return HandleResponse(response);
        }

        /// <summary>Отменить регистрацию текущего студента</summary>
        [HttpDelete("{id:int}/unregister")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Unregister(int id)
        {
            var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _eventService.UnregisterStudentAsync(id, studentId);
            return HandleResponse(response);
        }

        /// <summary>Снять регистрацию конкретного студента (для воспитателя)</summary>
        [HttpDelete("{id:int}/unregister/{studentId:int}")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> UnregisterStudent(int id, int studentId)
        {
            var response = await _eventService.UnregisterStudentAsync(id, studentId);
            return HandleResponse(response);
        }

        /// <summary>Удалить мероприятие</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _eventService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
