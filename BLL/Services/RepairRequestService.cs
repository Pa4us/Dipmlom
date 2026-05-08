using AutoMapper;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Repositories;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using SharedModel.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class RepairRequestService : BaseService<RepairRequest, RepairRequestDto, CreateRepairRequestDto, UpdateRepairRequestDto>, IRepairRequestService
    {
        private readonly IRepository<RepairComment> _commentRepository;
        private readonly IRepository<User> _userRepository;

        public RepairRequestService(
            IRepository<RepairRequest> repository,
            IRepository<RepairComment> commentRepository,
            IRepository<User> userRepository,
            IMapper mapper)
            : base(repository, mapper)
        {
            _commentRepository = commentRepository;
            _userRepository = userRepository;
        }

        private static readonly System.Linq.Expressions.Expression<Func<RepairRequest, object>>[] _includes =
        {
            r => r.Block,
            r => r.RequestedBy
        };

        public override async Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(_includes);
            return ApiResponse<IEnumerable<RepairRequestDto>>.Ok(_mapper.Map<IEnumerable<RepairRequestDto>>(entities));
        }

        public override async Task<ApiResponse<RepairRequestDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, _includes);
            if (entity == null) return ApiResponse<RepairRequestDto>.Fail($"Заявка с ID {id} не найдена");
            return ApiResponse<RepairRequestDto>.Ok(_mapper.Map<RepairRequestDto>(entity));
        }

        public override async Task<ApiResponse<RepairRequestDto>> CreateAsync(CreateRepairRequestDto createDto)
        {
            var request = _mapper.Map<RepairRequest>(createDto);
            request.Status = RequestStatus.Pending.ToString();
            request.CreatedAt = DateTime.Now;

            await _repository.AddAsync(request);
            await _repository.SaveChangesAsync();

            var dto = _mapper.Map<RepairRequestDto>(request);
            return ApiResponse<RepairRequestDto>.Ok(dto, "Заявка успешно создана");
        }

        public async Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByStatusAsync(string status)
        {
            var requests = await _repository.FindWithIncludeAsync(r => r.Status == status, _includes);
            return ApiResponse<IEnumerable<RepairRequestDto>>.Ok(_mapper.Map<IEnumerable<RepairRequestDto>>(requests));
        }

        public async Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByUserAsync(int userId)
        {
            var requests = await _repository.FindWithIncludeAsync(r => r.RequestedById == userId, _includes);
            var dtos = _mapper.Map<List<RepairRequestDto>>(requests);

            // Дополняем каждую заявку комментариями (слесарь оставляет комментарии о ходе работ)
            foreach (var dto in dtos)
            {
                var commentsResp = await GetCommentsAsync(dto.Id);
                dto.Comments = commentsResp.Data?.OrderBy(c => c.CreatedAt).ToList();
            }

            return ApiResponse<IEnumerable<RepairRequestDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsByBlockAsync(int blockId)
        {
            var requests = await _repository.FindWithIncludeAsync(r => r.BlockId == blockId, _includes);
            return ApiResponse<IEnumerable<RepairRequestDto>>.Ok(_mapper.Map<IEnumerable<RepairRequestDto>>(requests));
        }

        public async Task<ApiResponse<IEnumerable<RepairRequestDto>>> GetRequestsAssignedToMeAsync(int mechanicId)
        {
            var requests = await _repository.FindWithIncludeAsync(r => r.AssignedToId == mechanicId, _includes);
            return ApiResponse<IEnumerable<RepairRequestDto>>.Ok(_mapper.Map<IEnumerable<RepairRequestDto>>(requests));
        }

        public async Task<ApiResponse<RepairRequestDto>> UpdateStatusAsync(int requestId, string status, string? comment = null)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return ApiResponse<RepairRequestDto>.Fail("Заявка не найдена");

            request.Status = status;
            if (status == RequestStatus.Completed.ToString())
                request.CompletedAt = DateTime.Now;

            _repository.Update(request);
            await _repository.SaveChangesAsync();

            if (!string.IsNullOrEmpty(comment))
            {
                // Добавляем комментарий о смене статуса
                var commentEntity = new RepairComment
                {
                    RepairRequestId = requestId,
                    UserId = request.AssignedToId ?? 0,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };
                await _commentRepository.AddAsync(commentEntity);
                await _commentRepository.SaveChangesAsync();
            }

            var dto = _mapper.Map<RepairRequestDto>(request);
            return ApiResponse<RepairRequestDto>.Ok(dto, $"Статус изменен на {status}");
        }

        public async Task<ApiResponse<RepairRequestDto>> AssignToMechanicAsync(int requestId, int mechanicId)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return ApiResponse<RepairRequestDto>.Fail("Заявка не найдена");

            var mechanic = await _userRepository.GetByIdAsync(mechanicId);
            if (mechanic == null)
                return ApiResponse<RepairRequestDto>.Fail("Слесарь не найден");

            request.AssignedToId = mechanicId;
            _repository.Update(request);
            await _repository.SaveChangesAsync();

            var dto = _mapper.Map<RepairRequestDto>(request);
            return ApiResponse<RepairRequestDto>.Ok(dto, $"Заявка назначена слесарю {mechanic.FullName}");
        }

        public async Task<ApiResponse<RepairCommentDto>> AddCommentAsync(int requestId, int userId, string comment)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return ApiResponse<RepairCommentDto>.Fail("Заявка не найдена");

            var commentEntity = new RepairComment
            {
                RepairRequestId = requestId,
                UserId = userId,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            await _commentRepository.AddAsync(commentEntity);
            await _commentRepository.SaveChangesAsync();

            var dto = _mapper.Map<RepairCommentDto>(commentEntity);

            var user = await _userRepository.GetByIdAsync(userId);
            dto.UserName = user?.FullName ?? "Неизвестный";

            return ApiResponse<RepairCommentDto>.Ok(dto, "Комментарий добавлен");
        }

        public async Task<ApiResponse<IEnumerable<RepairCommentDto>>> GetCommentsAsync(int requestId)
        {
            var comments = await _commentRepository.FindAsync(c => c.RepairRequestId == requestId);
            var dtos = _mapper.Map<IEnumerable<RepairCommentDto>>(comments);

            // Загружаем имена пользователей
            foreach (var dto in dtos)
            {
                var user = await _userRepository.GetByIdAsync(dto.UserId);
                dto.UserName = user?.FullName ?? "Неизвестный";
            }

            return ApiResponse<IEnumerable<RepairCommentDto>>.Ok(dtos);
        }
    }
}
