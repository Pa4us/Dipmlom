using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class RepairRequestDto: BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BlockId { get; set; }
        public string? BlockNumber { get; set; }
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public int RequestedById { get; set; }
        public string? RequestedByName { get; set; }
        public int? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string Status { get; set; } = "Pending";
        public string Priority { get; set; } = "Normal";
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<RepairCommentDto>? Comments { get; set; }
    }

    public class CreateRepairRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BlockId { get; set; }
        public int? RoomId { get; set; }
        public int RequestedById { get; set; }  // заполняется из JWT токена в контроллере
        public string Priority { get; set; } = "Normal";
    }

    public class UpdateRepairRequestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
    }

    public class UpdateRepairRequestStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class AssignRepairRequestDto
    {
        public int RequestId { get; set; }
        public int MechanicId { get; set; }
    }

    public class RepairCommentDto
    {
        public int Id { get; set; }
        public int RepairRequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AddCommentDto
    {
        public int RepairRequestId { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
