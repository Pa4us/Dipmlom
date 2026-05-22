using SharedModel.DTOs;
using SharedModel.DTOs.Common;

namespace BLL.Interfaces;

public interface ICheckInService
{
    Task<ApiResponse<CheckInRequestDto>>         CreateRequestAsync(CreateCheckInRequestDto dto);
    Task<ApiResponse<IEnumerable<CheckInRequestDto>>> GetAllRequestsAsync();
    Task<ApiResponse<IEnumerable<CheckInRequestItemDto>>> GetPendingItemsAsync();
    Task<ApiResponse<ProcessCheckInResultDto>>   ProcessRequestAsync(int requestId, int educatorId);
    Task<ApiResponse<ProcessCheckInResultDto>>   ProcessItemsAsync(List<int> itemIds, int educatorId);
    Task<ApiResponse<int>>                       RejectItemsAsync(List<int> itemIds);
    Task<ApiResponse<IEnumerable<CheckInRequestItemDto>>> GetRecentlyCheckedInAsync(int days);
}
