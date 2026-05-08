using SharedModel.DTOs.Common;
using SharedModel.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IStatisticsService
    {
        Task<ApiResponse<DormitoryStatisticsDto>> GetDormitoryStatisticsAsync(int? weekNumber = null, int? year = null);
        Task<ApiResponse<FloorStatisticsDto>> GetFloorStatisticsAsync(int floor, int? weekNumber = null, int? year = null);
        Task<ApiResponse<BlockWeeklyScoreDto>> GetBlockWeeklyScoreAsync(int blockId, int weekNumber, int year);
        Task<ApiResponse<IEnumerable<BlockWeeklyScoreDto>>> GetBlockWeeklyScoresHistoryAsync(int blockId, int? weekFrom = null, int? weekTo = null);
        Task<ApiResponse<DormitoryStatisticsDto>> GetStatisticsForPeriodAsync(DateOnly startDate, DateOnly endDate);
        Task<ApiResponse<byte[]>> ExportStatisticsToExcelAsync(int? floor = null, int? weekNumber = null, int? year = null);

        /// <summary>
        /// Пересчитывает BlockWeeklyScore, FloorWeeklyStat, DormitoryWeeklyStat
        /// для недели, в которую попадает <paramref name="date"/>.
        /// Вызывается автоматически при сохранении новой проверки.
        /// </summary>
        Task<ApiResponse<bool>> RecalculateWeeklyStatsAsync(int blockId, DateOnly date);
    }
}
