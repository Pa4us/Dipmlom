using AutoMapper;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class EventService : BaseService<Event, EventDto, CreateEventDto, UpdateEventDto>, IEventService
    {
        private readonly IRepository<EventParticipant> _participantRepository;
        private readonly IRepository<User> _userRepository;

        public EventService(
            IRepository<Event> repository,
            IRepository<EventParticipant> participantRepository,
            IRepository<User> userRepository,
            IMapper mapper)
            : base(repository, mapper)
        {
            _participantRepository = participantRepository;
            _userRepository = userRepository;
        }

        private static readonly System.Linq.Expressions.Expression<Func<Event, object>>[] _includes =
        {
            e => e.Organizer,
            e => e.EventParticipants
        };

        public override async Task<ApiResponse<IEnumerable<EventDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(_includes);
            return ApiResponse<IEnumerable<EventDto>>.Ok(_mapper.Map<IEnumerable<EventDto>>(entities));
        }

        public override async Task<ApiResponse<EventDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, _includes);
            if (entity == null) return ApiResponse<EventDto>.Fail($"Мероприятие с ID {id} не найдено");
            return ApiResponse<EventDto>.Ok(_mapper.Map<EventDto>(entity));
        }

        public async Task<ApiResponse<IEnumerable<EventDto>>> GetUpcomingEventsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var events = await _repository.FindWithIncludeAsync(e => e.EventDate >= today, _includes);
            return ApiResponse<IEnumerable<EventDto>>.Ok(_mapper.Map<IEnumerable<EventDto>>(events));
        }

        public async Task<ApiResponse<IEnumerable<EventDto>>> GetEventsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            var events = await _repository.FindWithIncludeAsync(e => e.EventDate >= startDate && e.EventDate <= endDate, _includes);
            return ApiResponse<IEnumerable<EventDto>>.Ok(_mapper.Map<IEnumerable<EventDto>>(events));
        }

        public async Task<ApiResponse<EventDto>> RegisterStudentAsync(int eventId, int studentId)
        {
            var eventEntity = await _repository.GetByIdAsync(eventId);
            if (eventEntity == null)
                return ApiResponse<EventDto>.Fail("Мероприятие не найдено");

            var existing = await _participantRepository.FindAsync(p => p.EventId == eventId && p.UserId == studentId);
            if (existing.Any())
                return ApiResponse<EventDto>.Fail("Студент уже зарегистрирован на это мероприятие");

            var participant = new EventParticipant
            {
                EventId = eventId,
                UserId = studentId,
                PointsEarned = eventEntity.PointsAwarded ?? 0, // Если null, то 0
                ParticipatedAt = DateTime.Now
            };

            await _participantRepository.AddAsync(participant);
            await _participantRepository.SaveChangesAsync();

            var dto = _mapper.Map<EventDto>(eventEntity);
            return ApiResponse<EventDto>.Ok(dto, "Студент успешно зарегистрирован на мероприятие");
        }

        public async Task<ApiResponse<bool>> UnregisterStudentAsync(int eventId, int studentId)
        {
            var participants = await _participantRepository.FindAsync(p => p.EventId == eventId && p.UserId == studentId);
            var participant = participants.FirstOrDefault();

            if (participant == null)
                return ApiResponse<bool>.Fail("Регистрация не найдена");

            _participantRepository.Delete(participant);
            await _participantRepository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Регистрация отменена");
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetParticipantsAsync(int eventId)
        {
            var participants = await _participantRepository.FindAsync(p => p.EventId == eventId);
            var userIds = participants.Select(p => p.UserId);

            var users = new List<User>();
            foreach (var userId in userIds)
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                    users.Add(user);
            }

            var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
            return ApiResponse<IEnumerable<UserDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<bool>> CheckStudentRegisteredAsync(int eventId, int studentId)
        {
            var participants = await _participantRepository.FindAsync(p => p.EventId == eventId && p.UserId == studentId);
            return ApiResponse<bool>.Ok(participants.Any());
        }
    }
}
