using AutoMapper;
using BLL.Interfaces;
using BLL.Mapping;
using BLL.Services;
using DAL.Entities;
using DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharedModel.DTOs;

namespace UnitTests.Unit;

/// <summary>
/// Юнит-тесты для UserService.
/// IRepository и IJwtService заменены Moq-заглушками — БД не нужна.
/// </summary>
public class UserServiceTests
{
    // ── Вспомогательный метод: создаёт сервис с нужными моками ──────────────

    private static (UserService service,
                    Mock<IRepository<User>> userRepo,
                    Mock<IRepository<Residence>> resRepo,
                    Mock<IRepository<Role>> roleRepo,
                    Mock<IJwtService> jwtSvc)
        Build()
    {
        var userRepo = new Mock<IRepository<User>>();
        var resRepo  = new Mock<IRepository<Residence>>();
        var roleRepo = new Mock<IRepository<Role>>();
        var jwtSvc   = new Mock<IJwtService>();

        // AutoMapper 16 требует ILoggerFactory в DI
        var mapper = new ServiceCollection()
                         .AddLogging()
                         .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
                         .BuildServiceProvider()
                         .GetRequiredService<IMapper>();

        var service = new UserService(
            userRepo.Object, resRepo.Object, roleRepo.Object, mapper, jwtSvc.Object);

        return (service, userRepo, resRepo, roleRepo, jwtSvc);
    }

    // ── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        var (service, userRepo, _, _, jwtSvc) = Build();

        var role = new Role { Id = 1, Name = "Student" };
        var user = new User
        {
            Id           = 1,
            Username     = "ivanov",
            Email        = "ivanov@test.com",
            // SHA-256 хэш строки "secret123"
            PasswordHash = HashPassword("secret123"),
            IsActive     = true,
            Role         = role,
            RoleId       = 1,
            FullName     = "Иванов Иван"
        };

        userRepo
            .Setup(r => r.FindWithIncludeAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User> { user });

        jwtSvc
            .Setup(j => j.GenerateToken(It.IsAny<UserDto>()))
            .Returns("fake.jwt.token");

        var dto = new LoginDto { Login = "ivanov", Password = "secret123" };

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("fake.jwt.token", result.Data.Token);
        Assert.Equal("ivanov", result.Data.User.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFail()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        var user = new User
        {
            Id           = 1,
            Username     = "ivanov",
            PasswordHash = HashPassword("correctPassword"),
            IsActive     = true,
            Role         = new Role { Name = "Student" }
        };

        userRepo
            .Setup(r => r.FindWithIncludeAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User> { user });

        var dto = new LoginDto { Login = "ivanov", Password = "wrongPassword" };

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Неверный", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFail()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        userRepo
            .Setup(r => r.FindWithIncludeAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User>()); // пустой список — пользователь не найден

        var dto = new LoginDto { Login = "nonexistent", Password = "any" };

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginAsync_BlockedUser_ReturnsFail()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        var user = new User
        {
            Id           = 1,
            Username     = "blocked",
            PasswordHash = HashPassword("pass"),
            IsActive     = false, // заблокирован
            Role         = new Role { Name = "Student" }
        };

        userRepo
            .Setup(r => r.FindWithIncludeAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User> { user });

        var dto = new LoginDto { Login = "blocked", Password = "pass" };

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("заблокирована", result.Message);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewUser_ReturnsSuccess()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        // Нет пользователей с таким логином/email
        userRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new CreateUserDto
        {
            FullName = "Петров Пётр",
            Username = "petrov",
            Email    = "petrov@test.com",
            Password = "qwerty123",
            RoleId   = 1
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        // Убеждаемся, что AddAsync был вызван ровно один раз
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ReturnsFail()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        // Возвращаем существующего пользователя — дубликат
        userRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { new User { Username = "petrov" } });

        var dto = new CreateUserDto
        {
            FullName = "Другой Пётр",
            Username = "petrov",
            Email    = "other@test.com",
            Password = "pass",
            RoleId   = 1
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("уже существует", result.Message);
        // AddAsync не должен вызываться
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // ── ChangePasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_CorrectOldPassword_ReturnsSuccess()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        var user = new User { Id = 1, PasswordHash = HashPassword("oldPass") };
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await service.ChangePasswordAsync(1, "oldPass", "newPass");

        // Assert
        Assert.True(result.Success);
        // Хэш пароля изменился
        Assert.Equal(HashPassword("newPass"), user.PasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ReturnsFail()
    {
        // Arrange
        var (service, userRepo, _, _, _) = Build();

        var user = new User { Id = 1, PasswordHash = HashPassword("oldPass") };
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await service.ChangePasswordAsync(1, "wrongOld", "newPass");

        // Assert
        Assert.False(result.Success);
        userRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ── Вспомогательный метод: SHA-256 (тот же алгоритм, что в UserService) ─

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
