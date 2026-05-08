using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IBlockService: IBaseService<BlockDto, CreateBlockDto, UpdateBlockDto>
    {
        Task<ApiResponse<BlockDto>> GetBlockWithRoomsAsync(int id);
        Task<ApiResponse<IEnumerable<BlockDto>>> GetBlocksByFloorAsync(int floor);
        Task<ApiResponse<IEnumerable<BlockDto>>> GetBlocksByRangeAsync(int floorFrom, int floorTo);
    }
}
