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
    public class StatisticsService : IStatisticsService
    {
        private readonly IRepository<Block> _blockRepository;
        private readonly IRepository<BlockWeeklyScore> _scoreRepository;
        private readonly IRepository<Inspection> _inspectionRepository;
        private readonly IRepository<FloorWeeklyStat> _floorStatRepository;
        private readonly IRepository<DormitoryWeeklyStat> _dormStatRepository;
        private readonly IMapper _mapper;

        public StatisticsService(
            IRepository<Block> blockRepository,
            IRepository<BlockWeeklyScore> scoreRepository,
            IRepository<Inspection> inspectionRepository,
            IRepository<FloorWeeklyStat> floorStatRepository,
            IRepository<DormitoryWeeklyStat> dormStatRepository,
            IMapper mapper)
        {
            _blockRepository = blockRepository;
            _scoreRepository = scoreRepository;
            _inspectionRepository = inspectionRepository;
            _floorStatRepository = floorStatRepository;
            _dormStatRepository = dormStatRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<DormitoryStatisticsDto>> GetDormitoryStatisticsAsync(int? weekNumber = null, int? year = null)
        {
            var yearToUse = year ?? DateTime.Now.Year;
            var weekToUse = weekNumber ?? GetCurrentWeekNumber();

            var scores = await _scoreRepository.FindAsync(s => s.Year == yearToUse && s.WeekNumber == weekToUse);
            var averageScore = scores.Any() ? scores.Average(s => s.Score) : 0;

            var blocks = await _blockRepository.GetAllAsync();
            var floors = blocks.GroupBy(b => b.Floor).Select(g => g.Key).OrderBy(f => f);

            var floorStatistics = new List<FloorStatisticsDto>();
            foreach (var floor in floors)
            {
                var floorScores = scores.Where(s => s.Block.Floor == floor);
                var floorAvg = floorScores.Any() ? floorScores.Average(s => s.Score) : 0;

                floorStatistics.Add(new FloorStatisticsDto
                {
                    Floor = floor,
                    AverageScore = Math.Round(floorAvg, 2),
                    BlocksCount = blocks.Count(b => b.Floor == floor),
                    BlockScores = _mapper.Map<List<BlockWeeklyScoreDto>>(floorScores)
                });
            }

            var result = new DormitoryStatisticsDto
            {
                AverageScore = Math.Round(averageScore, 2),
                TotalBlocks = blocks.Count(),
                Floors = floorStatistics
            };

            return ApiResponse<DormitoryStatisticsDto>.Ok(result);
        }

        public async Task<ApiResponse<FloorStatisticsDto>> GetFloorStatisticsAsync(int floor, int? weekNumber = null, int? year = null)
        {
            var yearToUse = year ?? DateTime.Now.Year;
            var weekToUse = weekNumber ?? GetCurrentWeekNumber();

            var scores = await _scoreRepository.FindAsync(s => s.Year == yearToUse && s.WeekNumber == weekToUse && s.Block.Floor == floor);
            var averageScore = scores.Any() ? scores.Average(s => s.Score) : 0;
            var blocks = await _blockRepository.FindAsync(b => b.Floor == floor);

            var result = new FloorStatisticsDto
            {
                Floor = floor,
                AverageScore = Math.Round(averageScore, 2),
                BlocksCount = blocks.Count(),
                BlockScores = _mapper.Map<List<BlockWeeklyScoreDto>>(scores)
            };

            return ApiResponse<FloorStatisticsDto>.Ok(result);
        }

        public async Task<ApiResponse<BlockWeeklyScoreDto>> GetBlockWeeklyScoreAsync(int blockId, int weekNumber, int year)
        {
            var scores = await _scoreRepository.FindAsync(s => s.BlockId == blockId && s.WeekNumber == weekNumber && s.Year == year);
            var score = scores.FirstOrDefault();

            if (score == null)
                return ApiResponse<BlockWeeklyScoreDto>.Fail("Оценка не найдена");

            var dto = _mapper.Map<BlockWeeklyScoreDto>(score);
            return ApiResponse<BlockWeeklyScoreDto>.Ok(dto);
        }

        public async Task<ApiResponse<IEnumerable<BlockWeeklyScoreDto>>> GetBlockWeeklyScoresHistoryAsync(int blockId, int? weekFrom = null, int? weekTo = null)
        {
            var scores = await _scoreRepository.FindAsync(s => s.BlockId == blockId);

            if (weekFrom.HasValue)
                scores = scores.Where(s => s.WeekNumber >= weekFrom.Value);
            if (weekTo.HasValue)
                scores = scores.Where(s => s.WeekNumber <= weekTo.Value);

            scores = scores.OrderByDescending(s => s.Year).ThenByDescending(s => s.WeekNumber);

            var dtos = _mapper.Map<IEnumerable<BlockWeeklyScoreDto>>(scores);
            return ApiResponse<IEnumerable<BlockWeeklyScoreDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<DormitoryStatisticsDto>> GetStatisticsForPeriodAsync(DateOnly startDate, DateOnly endDate)
        {
            // Расчет статистики за произвольный период
            var allScores = await _scoreRepository.GetAllAsync();
            var filteredScores = allScores.Where(s => s.InspectionDate >= startDate && s.InspectionDate <= endDate);

            var averageScore = filteredScores.Any() ? filteredScores.Average(s => s.Score) : 0;
            var blocks = await _blockRepository.GetAllAsync();
            var floors = blocks.GroupBy(b => b.Floor).Select(g => g.Key).OrderBy(f => f);

            var floorStatistics = new List<FloorStatisticsDto>();
            foreach (var floor in floors)
            {
                var floorScores = filteredScores.Where(s => s.Block.Floor == floor);
                var floorAvg = floorScores.Any() ? floorScores.Average(s => s.Score) : 0;

                floorStatistics.Add(new FloorStatisticsDto
                {
                    Floor = floor,
                    AverageScore = Math.Round(floorAvg, 2),
                    BlocksCount = blocks.Count(b => b.Floor == floor),
                    BlockScores = _mapper.Map<List<BlockWeeklyScoreDto>>(floorScores)
                });
            }

            var result = new DormitoryStatisticsDto
            {
                AverageScore = Math.Round(averageScore, 2),
                TotalBlocks = blocks.Count(),
                Floors = floorStatistics
            };

            return ApiResponse<DormitoryStatisticsDto>.Ok(result);
        }

        public async Task<ApiResponse<byte[]>> ExportStatisticsToExcelAsync(int? floor = null, int? weekNumber = null, int? year = null)
        {
            // TODO: Реализовать экспорт в Excel
            // Пока возвращаем заглушку
            return ApiResponse<byte[]>.Fail("Экспорт в Excel временно недоступен");
        }

        public async Task<ApiResponse<bool>> RecalculateWeeklyStatsAsync(int blockId, DateOnly date)
        {
            try
            {
                var weekNumber = GetWeekNumber(date);
                var year = date.Year;
                var weekStart = GetWeekStart(date);
                var weekEnd = weekStart.AddDays(6);

                // ── 1. Пересчёт BlockWeeklyScore ────────────────────────────
                var inspections = await _inspectionRepository.FindAsync(i =>
                    i.BlockId == blockId &&
                    i.InspectionDate >= weekStart &&
                    i.InspectionDate <= weekEnd);

                var avgScore = inspections.Any()
                    ? (decimal)Math.Round(inspections.Average(i => i.Score), 2)
                    : 0m;

                var existingBlockScores = await _scoreRepository.FindAsync(s =>
                    s.BlockId == blockId && s.WeekNumber == weekNumber && s.Year == year);
                var blockScore = existingBlockScores.FirstOrDefault();

                if (blockScore == null)
                {
                    blockScore = new BlockWeeklyScore
                    {
                        BlockId = blockId,
                        WeekNumber = weekNumber,
                        Year = year,
                        Score = avgScore,
                        InspectionDate = weekEnd,   // дата конца недели
                        CreatedAt = DateTime.Now
                    };
                    await _scoreRepository.AddAsync(blockScore);
                }
                else
                {
                    blockScore.Score = avgScore;
                    _scoreRepository.Update(blockScore);
                }
                await _scoreRepository.SaveChangesAsync();

                // ── 2. Пересчёт FloorWeeklyStat ─────────────────────────────
                var block = await _blockRepository.GetByIdAsync(blockId);
                if (block == null) return ApiResponse<bool>.Fail("Блок не найден");

                var floorBlocks = await _blockRepository.FindAsync(b => b.Floor == block.Floor);
                var floorBlockIds = floorBlocks.Select(b => b.Id).ToList();

                var floorScores = await _scoreRepository.FindAsync(s =>
                    floorBlockIds.Contains(s.BlockId) &&
                    s.WeekNumber == weekNumber && s.Year == year);

                var floorAvg = floorScores.Any()
                    ? Math.Round(floorScores.Average(s => s.Score), 2)
                    : 0m;

                var existingFloorStats = await _floorStatRepository.FindAsync(s =>
                    s.Floor == block.Floor && s.WeekNumber == weekNumber && s.Year == year);
                var floorStat = existingFloorStats.FirstOrDefault();

                if (floorStat == null)
                {
                    floorStat = new FloorWeeklyStat
                    {
                        Floor = block.Floor,
                        WeekNumber = weekNumber,
                        Year = year,
                        AverageScore = floorAvg,
                        BlocksCount = floorBlocks.Count(),
                        CalculatedAt = DateTime.Now
                    };
                    await _floorStatRepository.AddAsync(floorStat);
                }
                else
                {
                    floorStat.AverageScore = floorAvg;
                    floorStat.CalculatedAt = DateTime.Now;
                    _floorStatRepository.Update(floorStat);
                }
                await _floorStatRepository.SaveChangesAsync();

                // ── 3. Пересчёт DormitoryWeeklyStat ────────────────────────
                var allBlocks = await _blockRepository.GetAllAsync();
                var allBlockIds = allBlocks.Select(b => b.Id).ToList();

                var allWeekScores = await _scoreRepository.FindAsync(s =>
                    allBlockIds.Contains(s.BlockId) &&
                    s.WeekNumber == weekNumber && s.Year == year);

                var dormAvg = allWeekScores.Any()
                    ? Math.Round(allWeekScores.Average(s => s.Score), 2)
                    : 0m;

                var existingDormStats = await _dormStatRepository.FindAsync(s =>
                    s.WeekNumber == weekNumber && s.Year == year);
                var dormStat = existingDormStats.FirstOrDefault();

                if (dormStat == null)
                {
                    dormStat = new DormitoryWeeklyStat
                    {
                        WeekNumber = weekNumber,
                        Year = year,
                        AverageScore = dormAvg,
                        TotalBlocks = allBlocks.Count(),
                        CalculatedAt = DateTime.Now
                    };
                    await _dormStatRepository.AddAsync(dormStat);
                }
                else
                {
                    dormStat.AverageScore = dormAvg;
                    dormStat.CalculatedAt = DateTime.Now;
                    _dormStatRepository.Update(dormStat);
                }
                await _dormStatRepository.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail($"Ошибка пересчёта статистики: {ex.Message}");
            }
        }

        private int GetCurrentWeekNumber()
        {
            return GetWeekNumber(DateOnly.FromDateTime(DateTime.Now));
        }

        private static int GetWeekNumber(DateOnly date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(
                date.ToDateTime(TimeOnly.MinValue),
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }

        private static DateOnly GetWeekStart(DateOnly date)
        {
            int dow = (int)date.DayOfWeek;
            int daysToMonday = dow == 0 ? 6 : dow - 1;
            return date.AddDays(-daysToMonday);
        }
    }
}
