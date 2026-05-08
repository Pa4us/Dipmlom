using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class BlockDto: BaseDto
    {
        public string BlockNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int BlockIndex { get; set; }
        public List<RoomDto>? Rooms { get; set; }
        public int RoomCount { get; set; }
    }

    public class CreateBlockDto
    {
        public string BlockNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int BlockIndex { get; set; }
    }

    public class UpdateBlockDto : CreateBlockDto
    {
        public int Id { get; set; }
    }
}
