using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class RoomDto: BaseDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int BlockId { get; set; }
        public string? BlockNumber { get; set; }
        public int Capacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public bool IsActive { get; set; }
        public int FreePlaces => Capacity - CurrentOccupancy;
    }

    public class CreateRoomDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int BlockId { get; set; }
        public int Capacity { get; set; } = 2;
    }

    public class UpdateRoomDto : CreateRoomDto
    {
        public int Id { get; set; }
        public int CurrentOccupancy { get; set; }
        public bool IsActive { get; set; }
    }
}
