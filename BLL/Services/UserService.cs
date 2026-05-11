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
        private readonly IRepository<Role>      _roleRepository;
        private readonly IJwtService _jwtService;

        public UserService(
            IRepository<User>      repository,
            IRepository<Residence> residenceRepository,
            IRepository<Role>      roleRepository,
            IMapper                mapper,
            IJwtService            jwtService)
            : base(repository, mapper)
        {
            _residenceRepository = residenceRepository;
            _roleRepository      = roleRepository;
            _jwtService          = jwtService;
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

        public override async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
        {
            var users = await _repository.GetAllWithIncludeAsync(u => u.Role);
            var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
            return ApiResponse<IEnumerable<UserDto>>.Ok(dtos);
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

        public async Task<ApiResponse<ImportUsersResultDto>> ImportUsersAsync(ImportUsersRequestDto dto)
        {
            // Загружаем всех существующих пользователей и роли за один запрос
            var allUsers = await _repository.GetAllAsync();
            var existingUsernames = allUsers.Select(u => u.Username.ToLower()).ToHashSet();
            var existingEmails    = allUsers.Select(u => u.Email.ToLower()).ToHashSet();

            var allRoles = await _roleRepository.GetAllAsync(); // IRepository<Role>
            var roleMap  = allRoles.ToDictionary(r => r.Name.ToLower(), r => r.Id);

            var result = new ImportUsersResultDto();

            // Логины/email внутри самого файла — проверяем дубликаты между строками
            var seenUsernames = new HashSet<string>();
            var seenEmails    = new HashSet<string>();

            foreach (var row in dto.Rows)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(row.FullName))  errors.Add("Не указано ФИО");
                if (string.IsNullOrWhiteSpace(row.Username))  errors.Add("Не указан логин");
                if (string.IsNullOrWhiteSpace(row.Email))     errors.Add("Не указан email");

                var uLow = row.Username?.ToLower() ?? "";
                var eLow = row.Email?.ToLower() ?? "";

                if (!string.IsNullOrEmpty(uLow))
                {
                    if (existingUsernames.Contains(uLow))       errors.Add("Логин уже занят");
                    else if (!seenUsernames.Add(uLow))          errors.Add("Логин дублируется в файле");
                }
                if (!string.IsNullOrEmpty(eLow))
                {
                    if (existingEmails.Contains(eLow))          errors.Add("Email уже используется");
                    else if (!seenEmails.Add(eLow))             errors.Add("Email дублируется в файле");
                }

                var roleName = (row.RoleName?.Trim() ?? "student").ToLower();
                if (!roleMap.ContainsKey(roleName))             errors.Add($"Роль «{row.RoleName}» не найдена");

                if (errors.Any())
                {
                    row.Error = string.Join("; ", errors);
                    result.InvalidRows.Add(row);
                    continue;
                }

                // Генерируем пароль (одинаковый при dry-run и при реальном создании)
                if (string.IsNullOrEmpty(row.GeneratedPassword))
                    row.GeneratedPassword = GeneratePassword();

                result.ValidRows.Add(row);

                if (!dto.DryRun)
                {
                    var user = new User
                    {
                        FullName     = row.FullName.Trim(),
                        Username     = row.Username.Trim(),
                        Email        = row.Email.Trim(),
                        RoleId       = roleMap[roleName],
                        PasswordHash = HashPassword(row.GeneratedPassword),
                        IsActive     = true,
                        CreatedAt    = DateTime.Now,
                    };
                    await _repository.AddAsync(user);
                    result.CreatedCount++;
                }
            }

            if (!dto.DryRun && result.CreatedCount > 0)
                await _repository.SaveChangesAsync();

            return ApiResponse<ImportUsersResultDto>.Ok(result,
                dto.DryRun
                    ? $"Проверка завершена: {result.ValidRows.Count} строк валидны, {result.InvalidRows.Count} с ошибками"
                    : $"Создано {result.CreatedCount} аккаунтов");
        }

        private static string GeneratePassword()
        {
            // Символы без визуально похожих: 0/O, 1/l/I
            const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}
