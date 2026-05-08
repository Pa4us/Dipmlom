using BLL.Interfaces;
using DAL.Repositories;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    public class StatisticsController : BaseApiController
    {
        private readonly IStatisticsService _statisticsService;
        private readonly IRepository<Inspection> _inspectionRepo;

        public StatisticsController(IStatisticsService statisticsService, IRepository<Inspection> inspectionRepo)
        {
            _statisticsService = statisticsService;
            _inspectionRepo = inspectionRepo;
        }

        /// <summary>Общая статистика общежития за неделю</summary>
        [HttpGet("dormitory")]
        public async Task<IActionResult> GetDormitory([FromQuery] int? week = null, [FromQuery] int? year = null)
        {
            var response = await _statisticsService.GetDormitoryStatisticsAsync(week, year);
            return HandleResponse(response);
        }

        /// <summary>Статистика по этажу за неделю</summary>
        [HttpGet("floor/{floor:int}")]
        public async Task<IActionResult> GetFloor(int floor, [FromQuery] int? week = null, [FromQuery] int? year = null)
        {
            var response = await _statisticsService.GetFloorStatisticsAsync(floor, week, year);
            return HandleResponse(response);
        }

        /// <summary>Недельная оценка блока</summary>
        [HttpGet("block/{blockId:int}/weekly")]
        public async Task<IActionResult> GetBlockWeekly(int blockId, [FromQuery] int week, [FromQuery] int year)
        {
            var response = await _statisticsService.GetBlockWeeklyScoreAsync(blockId, week, year);
            return HandleResponse(response);
        }

        /// <summary>История недельных оценок блока</summary>
        [HttpGet("block/{blockId:int}/history")]
        public async Task<IActionResult> GetBlockHistory(int blockId, [FromQuery] int? weekFrom = null, [FromQuery] int? weekTo = null)
        {
            var response = await _statisticsService.GetBlockWeeklyScoresHistoryAsync(blockId, weekFrom, weekTo);
            return HandleResponse(response);
        }

        /// <summary>Статистика за произвольный период</summary>
        [HttpGet("period")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> GetForPeriod([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var response = await _statisticsService.GetStatisticsForPeriodAsync(from, to);
            return HandleResponse(response);
        }

        /// <summary>Пересчитать статистику для всех блоков по всем проверкам (admin)</summary>
        [HttpPost("recalculate-all")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> RecalculateAll()
        {
            var inspections = await _inspectionRepo.GetAllAsync();
            var groups = inspections.GroupBy(i => (i.BlockId, i.InspectionDate));

            var errors = new List<string>();
            int count = 0;
            foreach (var g in groups)
            {
                var result = await _statisticsService.RecalculateWeeklyStatsAsync(g.Key.BlockId, g.Key.InspectionDate);
                if (!result.Success) errors.Add($"Block {g.Key.BlockId} / {g.Key.InspectionDate}: {result.Message}");
                else count++;
            }

            if (errors.Any())
                return Ok(new { success = false, message = $"Пересчитано {count}, ошибок: {errors.Count}", errors });

            return Ok(new { success = true, message = $"Пересчитано {count} групп проверок" });
        }

        /// <summary>Экспорт статистики в Excel</summary>
        [HttpGet("export")]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Export([FromQuery] int? floor = null, [FromQuery] int? week = null, [FromQuery] int? year = null)
        {
            var response = await _statisticsService.ExportStatisticsToExcelAsync(floor, week, year);
            if (!response.Success || response.Data == null)
                return BadRequest(response);

            return File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "statistics.xlsx");
        }
    }
}
