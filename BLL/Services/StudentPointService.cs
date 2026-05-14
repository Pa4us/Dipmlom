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
    public class StudentPointService : BaseService<StudentPoint, StudentPointDto, CreateStudentPointDto, UpdateStudentPointDto>, IStudentPointService
    {
        private readonly IRepository<User> _userRepository;

        public StudentPointService(IRepository<StudentPoint> repository, IRepository<User> userRepository, IMapper mapper)
            : base(repository, mapper)
        {
            _userRepository = userRepository;
        }

        public override async Task<ApiResponse<IEnumerable<StudentPointDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(p => p.User);
            return ApiResponse<IEnumerable<StudentPointDto>>.Ok(_mapper.Map<IEnumerable<StudentPointDto>>(entities));
        }

        public override async Task<ApiResponse<StudentPointDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, p => p.User);
            if (entity == null) return ApiResponse<StudentPointDto>.Fail($"Запись с ID {id} не найдена");
            return ApiResponse<StudentPointDto>.Ok(_mapper.Map<StudentPointDto>(entity));
        }

        public async Task<ApiResponse<StudentRatingDto>> GetStudentRatingAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<StudentRatingDto>.Fail("Студент не найден");

            var points = await _repository.FindAsync(p => p.UserId == userId);

            var eventPoints = points.Where(p => p.PointsType == "Award" && p.SourceType == "Event").Sum(p => p.Points);
            var penaltyPoints = points.Where(p => p.PointsType == "Penalty").Sum(p => p.Points);
            var totalPoints = points.Where(p => p.PointsType == "Award").Sum(p => p.Points) - penaltyPoints;

            var rating = new StudentRatingDto
            {
                UserId = userId,
                FullName = user.FullName,
                TotalPoints = totalPoints,
                EventPoints = eventPoints,
                PenaltyPoints = penaltyPoints,
                AverageCleanlinessScore = 0 // TODO: рассчитать из проверок
            };

            return ApiResponse<StudentRatingDto>.Ok(rating);
        }

        public async Task<ApiResponse<IEnumerable<StudentRatingDto>>> GetAllRatingsAsync()
        {
            var users = await _userRepository.FindWithIncludeAsync(
                u => u.Role.Name == "Student", u => u.Role);
            var ratings = new List<StudentRatingDto>();

            foreach (var user in users)
            {
                var ratingRes = await GetStudentRatingAsync(user.Id);
                if (ratingRes.Success && ratingRes.Data != null)
                    ratings.Add(ratingRes.Data);
            }

            ratings = ratings.OrderByDescending(r => r.TotalPoints).ToList();
            return ApiResponse<IEnumerable<StudentRatingDto>>.Ok(ratings);
        }

        public async Task<ApiResponse<IEnumerable<StudentRatingDto>>> GetTopStudentsAsync(int count)
        {
            var allRatingsRes = await GetAllRatingsAsync();
            if (!allRatingsRes.Success)
                return ApiResponse<IEnumerable<StudentRatingDto>>.Fail(allRatingsRes.Message);

            var top = allRatingsRes.Data?.Take(count) ?? new List<StudentRatingDto>();
            return ApiResponse<IEnumerable<StudentRatingDto>>.Ok(top);
        }

        public async Task<ApiResponse<StudentPointDto>> AwardPointsAsync(int userId, int points, string reason, string sourceType, int? sourceId = null)
        {
            var point = new StudentPoint
            {
                UserId = userId,
                Points = points,
                PointsType = "Award",
                SourceType = sourceType,
                SourceId = sourceId,
                Reason = reason,
                CreatedAt = DateTime.Now
            };

            await _repository.AddAsync(point);
            await _repository.SaveChangesAsync();

            var dto = _mapper.Map<StudentPointDto>(point);
            return ApiResponse<StudentPointDto>.Ok(dto, $"Начислено {points} баллов");
        }

        public async Task<ApiResponse<StudentPointDto>> DeductPointsAsync(int userId, int points, string reason, string sourceType, int? sourceId = null)
        {
            var point = new StudentPoint
            {
                UserId = userId,
                Points = points,
                PointsType = "Penalty",
                SourceType = sourceType,
                SourceId = sourceId,
                Reason = reason,
                CreatedAt = DateTime.Now
            };

            await _repository.AddAsync(point);
            await _repository.SaveChangesAsync();

            var dto = _mapper.Map<StudentPointDto>(point);
            return ApiResponse<StudentPointDto>.Ok(dto, $"Взыскано {points} баллов");
        }

        public async Task<ApiResponse<int>> GetTotalPointsByUserAsync(int userId)
        {
            var points = await _repository.FindAsync(p => p.UserId == userId);
            var total = points.Where(p => p.PointsType == "Award").Sum(p => p.Points)
                        - points.Where(p => p.PointsType == "Penalty").Sum(p => p.Points);
            return ApiResponse<int>.Ok(total);
        }

        public async Task<ApiResponse<IEnumerable<StudentPointDto>>> GetPointsByUserAsync(int userId)
        {
            var entities = await _repository.FindWithIncludeAsync(p => p.UserId == userId, p => p.User);
            var dtos = _mapper.Map<IEnumerable<StudentPointDto>>(entities);
            return ApiResponse<IEnumerable<StudentPointDto>>.Ok(dtos);
        }
    }
}
