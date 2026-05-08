using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IResidenceService: IBaseService<ResidenceDto, CreateResidenceDto, UpdateResidenceDto>
    {
        Task<ApiResponse<ResidenceDto>> GetCurrentResidenceByUserAsync(int userId);
        Task<ApiResponse<IEnumerable<ResidenceDto>>> GetResidenceHistoryByUserAsync(int userId);
        Task<ApiResponse<IEnumerable<ResidenceDto>>> GetCurrentResidentsByRoomAsync(int roomId);
        Task<ApiResponse<ResidenceDto>> CheckOutAsync(int residenceId, DateOnly moveOutDate);
    }
}
