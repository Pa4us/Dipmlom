using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IRoomService: IBaseService<RoomDto, CreateRoomDto, UpdateRoomDto>
    {
        Task<ApiResponse<IEnumerable<RoomDto>>> GetRoomsByBlockAsync(int blockId);
        Task<ApiResponse<IEnumerable<RoomDto>>> GetFreeRoomsAsync();
        Task<ApiResponse<bool>> UpdateOccupancyAsync(int roomId, int delta); // +1 или -1 при заселении/выселении
    }
}
