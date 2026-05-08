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
using System.Security.Cryptography;

namespace BLL.Services
{
    public class UserService: BaseService<User, UserDto, CreateUserDto, UpdateUserDto>, IUserService
    {
        private readonly IRepository<Residence> _residenceRepository;
        private readonly IJwtService _jwtService;

        public UserService(IRepository<User> repository, IRepository<Residence> residenceRepository, IMapper mapper, IJwtService jwtService)
            : base(repository, mapper)
        {
            _residenceRepository = residenceRepository;
            _jwtService = jwtService;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public override async Task<ApiResponse<UserDto>> CreateAsync(CreateUserDto createDto)
        {
            try
            {
                var existingUsers = await _repository.FindAsync(u => u.Username == createDto.Username || u.Email == createDto.Email);
                if (existingUsers.Any())
                    return ApiResponse<UserDto>.Fail("Пользователь с таким логином или email уже существует");

                var user = _mapper.Map<User>(createDto);
                user.PasswordHash = HashPassword(createDto.Password);
                user.CreatedAt = DateTime.Now;

                await _repository.AddAsync(user);
                await _repository.SaveChangesAsync();

                var dto = _mapper.Map<UserDto>(user);
                return ApiResponse<UserDto>.Ok(dto, "Пользователь успешно создан");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.Fail($"Ошибка при создании: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> GetByUsernameAsync(string username)
        {
            var users = await _repository.FindWithIncludeAsync(u => u.Username == username, u => u.Role);
            var user = users.FirstOrDefault();

            if (user == null)
                return ApiResponse<UserDto>.Fail($"Пользователь с логином '{username}' не найден");

            var dto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.Ok(dto);
        }

        public async Task<ApiResponse<UserDto>> GetByEmailAsync(string email)
        {
            var users = await _repository.FindWithIncludeAsync(u => u.Email == email, u => u.Role);
            var user = users.FirstOrDefault();

            if (user == null)
                return ApiResponse<UserDto>.Fail($"Пользователь с email '{email}' не найден");

            var dto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.Ok(dto);
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto)
        {
            // Поддержка входа по логину ИЛИ email
            var login = loginDto.Login.Trim();
            var users = await _repository.FindWithIncludeAsync(
                u => u.Username == login || u.Email == login,
                u => u.Role);
            var user = users.FirstOrDefault();

            if (user == null)
                return ApiResponse<LoginResponseDto>.Fail("Неверный логин или пароль");

            var hashedPassword = HashPassword(loginDto.Password);
            if (user.PasswordHash != hashedPassword)
                return ApiResponse<LoginResponseDto>.Fail("Неверный логин или пароль");

            if (user.IsActive == false)  // Исправлено: сравнение с false вместо !user.IsActive
                return ApiResponse<LoginResponseDto>.Fail("Учетная запись заблокирована");

            var userDto = _mapper.Map<UserDto>(user);
            var token = _jwtService.GenerateToken(userDto);

            return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetUsersByRoleAsync(int roleId)
        {
            var users = await _repository.FindWithIncludeAsync(u => u.RoleId == roleId, u => u.Role);
            var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
            return ApiResponse<IEnumerable<UserDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetResidentsByBlockAsync(int blockId)
        {
            var residences = await _residenceRepository.FindAsync(r => r.BlockId == blockId && r.IsCurrent == true);
            var userIds = residences.Select(r => r.UserId).ToHashSet();

            var users = await _repository.FindWithIncludeAsync(u => userIds.Contains(u.Id), u => u.Role);

            var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
            return ApiResponse<IEnumerable<UserDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.Fail("Пользователь не найден");

            var oldHashed = HashPassword(oldPassword);
            if (user.PasswordHash != oldHashed)
                return ApiResponse<bool>.Fail("Неверный текущий пароль");

            user.PasswordHash = HashPassword(newPassword);
            _repository.Update(user);
            await _repository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Пароль успешно изменен");
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string email, string newPassword)
        {
            var users = await _repository.FindAsync(u => u.Email == email);
            var user = users.FirstOrDefault();
            if (user == null)
                return ApiResponse<bool>.Fail("Пользователь не найден");

            user.PasswordHash = HashPassword(newPassword);
            _repository.Update(user);
            await _repository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Пароль успешно изменён");
        }

        public async Task<ApiResponse<bool>> AdminResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.Fail("Пользователь не найден");

            user.PasswordHash = HashPassword(newPassword);
            _repository.Update(user);
            await _repository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Пароль сброшен");
        }
    }
}
