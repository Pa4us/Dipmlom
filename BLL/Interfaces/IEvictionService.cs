using SharedModel.DTOs;
using SharedModel.DTOs.Common;

namespace BLL.Interfaces;

public interface IEvictionService
{
    Task<ApiResponse<IEnumerable<EvictionRequestDto>>> CreateRequestsAsync(CreateEvictionRequestDto dto);
    Task<ApiResponse<IEnumerable<EvictionRequestDto>>> GetPendingAsync();
    Task<ApiResponse<bool>>                            ConfirmBatchAsync(ConfirmEvictionDto dto);
}
