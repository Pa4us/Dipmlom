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
    public class InspectionService : BaseService<Inspection, InspectionDto, CreateInspectionDto, UpdateInspectionDto>, IInspectionService
    {
        private readonly IRepository<Block> _blockRepository;
        private readonly IStatisticsService _statisticsService;

        public InspectionService(
            IRepository<Inspection> repository,
            IRepository<Block> blockRepository,
            IStatisticsService statisticsService,
            IMapper mapper)
            : base(repository, mapper)
        {
            _blockRepository = blockRepository;
            _statisticsService = statisticsService;
        }

        // Навигационные свойства для Include
        private static readonly System.Linq.Expressions.Expression<Func<Inspection, object>>[] _includes =
        {
            i => i.Block,
            i => i.Zone,
            i => i.Inspector,
            i => i.Room!   // nullable — Include всё равно нужен для маппинга RoomNumber
        };

        public override async Task<ApiResponse<IEnumerable<InspectionDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithIncludeAsync(_includes);
            return ApiResponse<IEnumerable<InspectionDto>>.Ok(_mapper.Map<IEnumerable<InspectionDto>>(entities));
        }

        public override async Task<ApiResponse<InspectionDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithIncludeAsync(id, _includes);
            if (entity == null) return ApiResponse<InspectionDto>.Fail($"Проверка с ID {id} не найдена");
            return ApiResponse<InspectionDto>.Ok(_mapper.Map<InspectionDto>(entity));
        }

        public override async Task<ApiResponse<InspectionDto>> CreateAsync(CreateInspectionDto createDto)
        {
            // Ограничение: одна проверка в разрешённый день на одного инспектора
            var existing = await _repository.FindAsync(i =>
                i.InspectorId == createDto.InspectorId &&
                i.InspectionDate == createDto.InspectionDate);

            if (existing.Any())
                return ApiResponse<InspectionDto>.Fail("Вы уже проводили проверку сегодня. Разрешена только одна проверка в день.");

            try
            {
                var entity = _mapper.Map<Inspection>(createDto);
                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                // Перезагружаем сущность с навигационными свойствами для маппинга
                var saved = await _repository.GetByIdWithIncludeAsync(entity.Id, _includes);
                var dto = _mapper.Map<InspectionDto>(saved!);

                // Пересчитываем статистику блока
                await _statisticsService.RecalculateWeeklyStatsAsync(
                    createDto.BlockId,
                    createDto.InspectionDate);

                return ApiResponse<InspectionDto>.Ok(dto, "Проверка успешно создана");
            }
            catch (Exception ex)
            {
                return ApiResponse<InspectionDto>.Fail($"Ошибка при создании: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByBlockAsync(int blockId)
        {
            var inspections = await _repository.FindWithIncludeAsync(i => i.BlockId == blockId, _includes);
            return ApiResponse<IEnumerable<InspectionDto>>.Ok(_mapper.Map<IEnumerable<InspectionDto>>(inspections));
        }

        public async Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByInspectorAsync(int inspectorId)
        {
            var inspections = await _repository.FindWithIncludeAsync(i => i.InspectorId == inspectorId, _includes);
            return ApiResponse<IEnumerable<InspectionDto>>.Ok(_mapper.Map<IEnumerable<InspectionDto>>(inspections));
        }

        public async Task<ApiResponse<IEnumerable<InspectionDto>>> GetInspectionsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            var inspections = await _repository.FindWithIncludeAsync(i => i.InspectionDate >= startDate && i.InspectionDate <= endDate, _includes);
            return ApiResponse<IEnumerable<InspectionDto>>.Ok(_mapper.Map<IEnumerable<InspectionDto>>(inspections));
        }

        public async Task<ApiResponse<InspectionReportDto>> GetInspectionReportByBlockAsync(int blockId, DateOnly? dateFrom = null, DateOnly? dateTo = null)
        {
            var block = await _blockRepository.GetByIdAsync(blockId);
            if (block == null)
                return ApiResponse<InspectionReportDto>.Fail("Блок не найден");

            var inspections = await _repository.FindAsync(i => i.BlockId == blockId);

            if (dateFrom.HasValue)
                inspections = inspections.Where(i => i.InspectionDate >= dateFrom.Value);
            if (dateTo.HasValue)
                inspections = inspections.Where(i => i.InspectionDate <= dateTo.Value);

            var averageScore = inspections.Any() ? (int)inspections.Average(i => i.Score) : 0;

            var report = new InspectionReportDto
            {
                BlockId = blockId,
                BlockNumber = block.BlockNumber,
                AverageScore = averageScore,
                Inspections = _mapper.Map<List<InspectionDto>>(inspections)
            };

            return ApiResponse<InspectionReportDto>.Ok(report);
        }

        public async Task<ApiResponse<decimal>> GetAverageScoreByBlockAsync(int blockId, DateOnly? dateFrom = null, DateOnly? dateTo = null)
        {
            var inspections = await _repository.FindAsync(i => i.BlockId == blockId);

            if (dateFrom.HasValue)
                inspections = inspections.Where(i => i.InspectionDate >= dateFrom.Value);
            if (dateTo.HasValue)
                inspections = inspections.Where(i => i.InspectionDate <= dateTo.Value);

            var average = inspections.Any() ? (decimal)inspections.Average(i => i.Score) : 0m;
            return ApiResponse<decimal>.Ok(Math.Round(average, 2));
        }
    }
}
