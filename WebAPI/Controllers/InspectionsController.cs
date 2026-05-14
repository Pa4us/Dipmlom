using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Authorize]
    public class InspectionsController : BaseApiController
    {
        private readonly IInspectionService _inspectionService;
        private readonly IRepository<InspectionZone> _zoneRepo;

        public InspectionsController(IInspectionService inspectionService, IRepository<InspectionZone> zoneRepo)
        {
            _inspectionService = inspectionService;
            _zoneRepo = zoneRepo;
        }

        /// <summary>Получить список зон проверки</summary>
        [HttpGet("zones")]
        public async Task<IActionResult> GetZones()
        {
            var zones = await _zoneRepo.GetAllAsync();
            var result = zones.Select(z => new { z.Id, z.Name, displayName = z.DisplayName });
            return Ok(new { success = true, data = result });
        }

        /// <summary>Получить все проверки</summary>
        [HttpGet]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _inspectionService.GetAllAsync();
            return HandleResponse(response);
        }

        /// <summary>Получить проверку по ID</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _inspectionService.GetByIdAsync(id);
            return HandleResponseNotFound(response);
        }

        /// <summary>Получить проверки блока</summary>
        [HttpGet("by-block/{blockId:int}")]
        public async Task<IActionResult> GetByBlock(int blockId)
        {
            var response = await _inspectionService.GetInspectionsByBlockAsync(blockId);
            return HandleResponse(response);
        }

        /// <summary>Получить проверки за период</summary>
        [HttpGet("by-date")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var response = await _inspectionService.GetInspectionsByDateRangeAsync(from, to);
            return HandleResponse(response);
        }

        /// <summary>Получить отчёт по блоку</summary>
        [HttpGet("report/block/{blockId:int}")]
        [Authorize(Roles = "Educator,Inspector")]
        public async Task<IActionResult> GetReportByBlock(int blockId, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
        {
            var response = await _inspectionService.GetInspectionReportByBlockAsync(blockId, from, to);
            return HandleResponse(response);
        }

        /// <summary>Средний балл блока</summary>
        [HttpGet("average/block/{blockId:int}")]
        public async Task<IActionResult> GetAverageByBlock(int blockId, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
        {
            var response = await _inspectionService.GetAverageScoreByBlockAsync(blockId, from, to);
            return HandleResponse(response);
        }

        /// <summary>Создать проверку (инспектор)</summary>
        [HttpPost]
        [Authorize(Roles = "Inspector,Educator")]
        public async Task<IActionResult> Create([FromBody] CreateInspectionDto dto)
        {
            // InspectorId берём из JWT токена
            dto.InspectorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _inspectionService.CreateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Обновить проверку</summary>
        [HttpPut]
        [Authorize(Roles = "Inspector,Educator")]
        public async Task<IActionResult> Update([FromBody] UpdateInspectionDto dto)
        {
            var response = await _inspectionService.UpdateAsync(dto);
            return HandleResponse(response);
        }

        /// <summary>Удалить проверку</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _inspectionService.DeleteAsync(id);
            return HandleResponse(response);
        }
    }
}
