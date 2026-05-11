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
/// Юнит-тесты для RepairRequestService.
/// Проверяем смену статуса, назначение слесаря, создание заявки.
/// </summary>
public class RepairRequestServiceTests
{
    private static (RepairRequestService service,
                    Mock<IRepository<RepairRequest>> reqRepo,
                    Mock<IRepository<RepairComment>> commentRepo,
                    Mock<IRepository<User>> userRepo)
    Build()
    {
        var reqRepo     = new Mock<IRepository<RepairRequest>>();
        var commentRepo = new Mock<IRepository<RepairComment>>();
        var userRepo    = new Mock<IRepository<User>>();
        var mapper      = new ServiceCollection()
                              .AddLogging()
                              .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
                              .BuildServiceProvider()
                              .GetRequiredService<IMapper>();

        var service = new RepairRequestService(
            reqRepo.Object, commentRepo.Object, userRepo.Object, mapper);

        return (service, reqRepo, commentRepo, userRepo);
    }

    // ── UpdateStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_PendingToInProgress_UpdatesStatus()
    {
        // Arrange
        var (service, reqRepo, _, _) = Build();

        var request = new RepairRequest
        {
            Id     = 1,
            Status = "Pending",
            Block  = new Block { BlockNumber = "1" },
            RequestedBy = new User { FullName = "Иванов" }
        };

        reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        reqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateStatusAsync(1, "InProgress");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("InProgress", request.Status);
        Assert.Null(request.CompletedAt); // CompletedAt не устанавливается для InProgress
        reqRepo.Verify(r => r.Update(request), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var (service, reqRepo, _, _) = Build();

        var request = new RepairRequest
        {
            Id     = 2,
            Status = "InProgress",
            Block  = new Block { BlockNumber = "2" },
            RequestedBy = new User { FullName = "Петров" }
        };

        reqRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(request);
        reqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.Now;

        // Act
        var result = await service.UpdateStatusAsync(2, "Completed");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Completed", request.Status);
        Assert.NotNull(request.CompletedAt);
        Assert.True(request.CompletedAt >= before); // CompletedAt установлен только что
    }

    [Fact]
    public async Task UpdateStatusAsync_RequestNotFound_ReturnsFail()
    {
        // Arrange
        var (service, reqRepo, _, _) = Build();

        reqRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((RepairRequest?)null);

        // Act
        var result = await service.UpdateStatusAsync(999, "Completed");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не найдена", result.Message);
        reqRepo.Verify(r => r.Update(It.IsAny<RepairRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithComment_SavesComment()
    {
        // Arrange
        var (service, reqRepo, commentRepo, _) = Build();

        var request = new RepairRequest
        {
            Id           = 3,
            Status       = "InProgress",
            AssignedToId = 5, // слесарь
            Block        = new Block { BlockNumber = "1" },
            RequestedBy  = new User { FullName = "Сидоров" }
        };

        reqRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(request);
        reqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        commentRepo.Setup(r => r.AddAsync(It.IsAny<RepairComment>())).Returns(Task.CompletedTask);
        commentRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateStatusAsync(3, "Completed", "Ремонт выполнен");

        // Assert
        Assert.True(result.Success);
        // Комментарий должен быть сохранён
        commentRepo.Verify(r => r.AddAsync(
            It.Is<RepairComment>(c =>
                c.RepairRequestId == 3 &&
                c.Comment == "Ремонт выполнен")),
            Times.Once);
    }

    // ── AssignToMechanicAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AssignToMechanicAsync_ValidIds_AssignsMechanic()
    {
        // Arrange
        var (service, reqRepo, _, userRepo) = Build();

        var request  = new RepairRequest { Id = 1, Status = "Pending",
                            Block = new Block { BlockNumber = "1" },
                            RequestedBy = new User { FullName = "Иванов" } };
        var mechanic = new User { Id = 7, FullName = "Слесарь Вася" };

        reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        userRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(mechanic);
        reqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.AssignToMechanicAsync(1, 7);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(7, request.AssignedToId);
        Assert.Contains("Слесарь Вася", result.Message);
    }

    [Fact]
    public async Task AssignToMechanicAsync_MechanicNotFound_ReturnsFail()
    {
        // Arrange
        var (service, reqRepo, _, userRepo) = Build();

        var request = new RepairRequest { Id = 1, Status = "Pending",
                          Block = new Block { BlockNumber = "1" },
                          RequestedBy = new User { FullName = "Иванов" } };

        reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act
        var result = await service.AssignToMechanicAsync(1, 999);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Слесарь не найден", result.Message);
        reqRepo.Verify(r => r.Update(It.IsAny<RepairRequest>()), Times.Never);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewRequest_StatusIsPending()
    {
        // Arrange
        var (service, reqRepo, _, _) = Build();

        RepairRequest? savedRequest = null;
        reqRepo.Setup(r => r.AddAsync(It.IsAny<RepairRequest>()))
               .Callback<RepairRequest>(r => savedRequest = r)
               .Returns(Task.CompletedTask);
        reqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new CreateRepairRequestDto
        {
            Title         = "Сломан кран",
            Description   = "Течёт горячая вода",
            BlockId       = 1,
            RequestedById = 2
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(savedRequest);
        Assert.Equal("Pending", savedRequest!.Status); // новая заявка всегда Pending
        Assert.True(savedRequest.CreatedAt > DateTime.MinValue);
    }
}
