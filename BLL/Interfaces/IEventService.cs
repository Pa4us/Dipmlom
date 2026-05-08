using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IEventService: IBaseService<EventDto, CreateEventDto, UpdateEventDto>
    {
        Task<ApiResponse<IEnumerable<EventDto>>> GetUpcomingEventsAsync();
        Task<ApiResponse<IEnumerable<EventDto>>> GetEventsByDateRangeAsync(DateOnly startDate, DateOnly endDate);
        Task<ApiResponse<EventDto>> RegisterStudentAsync(int eventId, int studentId);
        Task<ApiResponse<bool>> UnregisterStudentAsync(int eventId, int studentId);
        Task<ApiResponse<IEnumerable<UserDto>>> GetParticipantsAsync(int eventId);
        Task<ApiResponse<bool>> CheckStudentRegisteredAsync(int eventId, int studentId);
    }
}
