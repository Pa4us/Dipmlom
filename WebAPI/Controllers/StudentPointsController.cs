using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Authorize]
    public class StudentPointsController : BaseApiController
    {
        private readonly IStudentPointService _studentPointService;

        public StudentPointsController(IStudentPointService studentPointService)
        {
            _studentPointService = studentPointService;
        }

        /// <summary>Рейтинг всех студентов</summary>
        [HttpGet("ratings")]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> GetAllRatings()
        {
            var response = await _studentPointService.GetAllRatingsAsync();
            return HandleResponse(response);
        }

        /// <summary>Топ N студентов</summary>
        [HttpGet("ratings/top/{count:int}")]
        public async Task<IActionResult> GetTop(int count)
        {
            var response = await _studentPointService.GetTopStudentsAsync(count);
            return HandleResponse(response);
        }

        /// <summary>Рейтинг конкретного студента</summary>
        [HttpGet("ratings/{userId:int}")]
        public async Task<IActionResult> GetRating(int userId)
        {
            var response = await _studentPointService.GetStudentRatingAsync(userId);
            return HandleResponseNotFound(response);
        }

        /// <summary>Мой рейтинг</summary>
        [HttpGet("ratings/my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyRating()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _studentPointService.GetStudentRatingAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Сумма баллов студента</summary>
        [HttpGet("total/{userId:int}")]
        public async Task<IActionResult> GetTotal(int userId)
        {
            var response = await _studentPointService.GetTotalPointsByUserAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Начислить баллы студенту</summary>
        [HttpPost("award")]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> Award([FromBody] AwardPointsDto dto)
        {
            var response = await _studentPointService.AwardPointsAsync(dto.UserId, dto.Points, dto.Reason, dto.SourceType, dto.SourceId);
            return HandleResponse(response);
        }

        /// <summary>Взыскать баллы со студента</summary>
        [HttpPost("deduct")]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> Deduct([FromBody] DeductPointsDto dto)
        {
            var response = await _studentPointService.DeductPointsAsync(dto.UserId, dto.Points, dto.Reason, dto.SourceType, dto.SourceId);
            return HandleResponse(response);
        }

        /// <summary>Все записи о баллах</summary>
        [HttpGet]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _studentPointService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Мои баллы (для любого авторизованного пользователя)</summary>
        [HttpGet("my-points")]
        public async Task<IActionResult> GetMyPoints()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _studentPointService.GetPointsByUserAsync(userId);
            return HandleResponse(response);
        }

        /// <summary>Удалить запись о баллах</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _studentPointService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
