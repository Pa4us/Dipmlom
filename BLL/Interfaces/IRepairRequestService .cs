using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IRepairRequestService: IBaseService<RepairRequestDto, CreateRepairRequestDto, UpdateRepairRequestDto>
    {
        Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByStatusAsync(string status);
        Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByUserAsync(int userId);
        Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByBlockAsync(int blockId);
        Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsAssignedToMeAsync(int mechanicId);
        Task<ApiResponse<RepairRequestDto>> UpdateStatusAsync(int requestId, string status, string? comment = null);
        Task<ApiResponse<RepairRequestDto>> AssignToMechanicAsync(int requestId, int mechanicId);
        Task<ApiResponse<RepairCommentDto>> AddCommentAsync(int requestId, int userId, string comment);
        Task<ApiResponse<IEnumerable<RepairCommentDto>>> GetCommentsAsync(int requestId);
    }
}
