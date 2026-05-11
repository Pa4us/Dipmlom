using AutoMapper;
using BLL.Mapping;
using BLL.Services;
using DAL.Entities;
using DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharedModel.DTOs;

namespace UnitTests.Unit;

/// <summary>
/// Юнит-тесты для StudentPointService.
/// Проверяем начисление/снятие баллов и расчёт рейтинга без обращения к БД.
/// </summary>
public class StudentPointServiceTests
{
    private static (StudentPointService service,
                    Mock<IRepository<StudentPoint>> pointRepo,
                    Mock<IRepository<User>> userRepo)
        Build()
    {
        var pointRepo = new Mock<IRepository<StudentPoint>>();
        var userRepo  = new Mock<IRepository<User>>();
        var mapper    = new ServiceCollection()
                            .AddLogging()
                            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
                            .BuildServiceProvider()
                            .GetRequiredService<IMapper>();

        var service = new StudentPointService(pointRepo.Object, userRepo.Object, mapper);
        return (service, pointRepo, userRepo);
    }

    // ── AwardPointsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AwardPointsAsync_ValidData_SavesPointAndReturnsSuccess()
    {
        // Arrange
        var (service, pointRepo, _) = Build();

        pointRepo.Setup(r => r.AddAsync(It.IsAny<StudentPoint>())).Returns(Task.CompletedTask);
        pointRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.AwardPointsAsync(
            userId: 1, points: 10, reason: "За участие в мероприятии",
            sourceType: "Manual");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("10", result.Message); // "Начислено 10 баллов"

        // Проверяем, что запись сохранена с типом Award
        pointRepo.Verify(r => r.AddAsync(
            It.Is<StudentPoint>(p =>
                p.UserId     == 1     &&
                p.Points     == 10    &&
                p.PointsType == "Award")),
            Times.Once);
        pointRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeductPointsAsync_ValidData_SavesPenaltyRecord()
    {
        // Arrange
        var (service, pointRepo, _) = Build();

        pointRepo.Setup(r => r.AddAsync(It.IsAny<StudentPoint>())).Returns(Task.CompletedTask);
        pointRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.DeductPointsAsync(
            userId: 2, points: 5, reason: "Нарушение правил",
            sourceType: "Manual");

        // Assert
        Assert.True(result.Success);

        pointRepo.Verify(r => r.AddAsync(
            It.Is<StudentPoint>(p =>
                p.UserId     == 2       &&
                p.Points     == 5       &&
                p.PointsType == "Penalty")),
            Times.Once);
    }

    // ── GetStudentRatingAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetStudentRatingAsync_CorrectlyCalculatesTotalPoints()
    {
        // Arrange
        var (service, pointRepo, userRepo) = Build();

        userRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new User { Id = 1, FullName = "Иванов Иван" });

        // У студента: +15 Award (из них 10 за мероприятие), -3 Penalty
        // Итого: 15 - 3 = 12 баллов
        pointRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentPoint, bool>>>()))
                 .ReturnsAsync(new List<StudentPoint>
                 {
                     new() { UserId = 1, Points = 10, PointsType = "Award",   SourceType = "Event" },
                     new() { UserId = 1, Points = 5,  PointsType = "Award",   SourceType = "Manual" },
                     new() { UserId = 1, Points = 3,  PointsType = "Penalty", SourceType = "Manual" },
                 });

        // Act
        var result = await service.GetStudentRatingAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(12, result.Data!.TotalPoints);   // 15 - 3
        Assert.Equal(10, result.Data.EventPoints);    // только Award + Event
        Assert.Equal(3,  result.Data.PenaltyPoints);
    }

    [Fact]
    public async Task GetStudentRatingAsync_UserNotFound_ReturnsFail()
    {
        // Arrange
        var (service, _, userRepo) = Build();

        userRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((User?)null); // пользователь не найден

        // Act
        var result = await service.GetStudentRatingAsync(999);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не найден", result.Message);
    }

    // ── GetTotalPointsByUserAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData(20, 5,  15)]  // 20 начислено, 5 снято → итого 15
    [InlineData(0,  0,  0)]   // нет баллов вообще → 0
    [InlineData(10, 10, 0)]   // начислено = снято → 0
    [InlineData(5,  10, -5)]  // штрафов больше, чем баллов → отрицательный
    public async Task GetTotalPointsByUserAsync_VariousScenarios(
        int awardPoints, int penaltyPoints, int expectedTotal)
    {
        // Arrange
        var (service, pointRepo, _) = Build();

        var points = new List<StudentPoint>();
        if (awardPoints > 0)
            points.Add(new StudentPoint { UserId = 1, Points = awardPoints, PointsType = "Award" });
        if (penaltyPoints > 0)
            points.Add(new StudentPoint { UserId = 1, Points = penaltyPoints, PointsType = "Penalty" });

        pointRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentPoint, bool>>>()))
                 .ReturnsAsync(points);

        // Act
        var result = await service.GetTotalPointsByUserAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedTotal, result.Data);
    }
}
