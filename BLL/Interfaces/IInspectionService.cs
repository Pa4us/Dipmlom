using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IInspectionService: IBaseService<InspectionDto, CreateInspectionDto, UpdateInspectionDto>
    {
        Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByBlockAsync(int blockId);
        Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByInspectorAsync(int inspectorId);
        Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByDateRangeAsync(DateOnly startDate, DateOnly endDate);
        Task<ApiResponse<InspectionReportDto>> GetInspectionReportByBlockAsync(int blockId, DateOnly? dateFrom = null, DateOnly? dateTo = null);
        Task<ApiResponse<decimal>> GetAverageScoreByBlockAsync(int blockId, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    }
}

