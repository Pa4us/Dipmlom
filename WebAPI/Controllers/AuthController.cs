using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using SharedModel.DTOs.Common;
using System.Security.Claims;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetService _resetService;
        private readonly IConfiguration _config;

        public AuthController(
            IUserService userService,
            IEmailService emailService,
            IPasswordResetService resetService,
            IConfiguration config)
        {
            _userService  = userService;
            _emailService = emailService;
            _resetService = resetService;
            _config       = config;
        }

        /// <summary>Вход в систему (по логину или email)</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var response = await _userService.LoginAsync(loginDto);
            return HandleResponse(response);
        }

        /// <summary>Регистрация нового пользователя</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createDto)
        {
            var response = await _userService.CreateAsync(createDto);
            return HandleResponse(response);
        }

        /// <summary>Изменение пароля текущего пользователя</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
            return HandleResponse(response);
        }

        /// <summary>Получить информацию о текущем пользователе</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _userService.GetByIdAsync(userId);
            return HandleResponseNotFound(response);
        }

        /// <summary>Запрос сброса пароля — генерирует токен и отправляет письмо</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            // Ищем пользователя, но не раскрываем наличие/отсутствие email
            var userResp = await _userService.GetByEmailAsync(dto.Email.Trim());
            if (userResp.Data == null)
                return Ok(ApiResponse<bool>.Ok(true, "Если email зарегистрирован — письмо отправлено"));

            var token    = _resetService.GenerateToken(dto.Email.Trim());
            var webAppUrl = _config["AppSettings:WebAppUrl"]?.TrimEnd('/') ?? "http://localhost:5001";
            var resetLink = $"{webAppUrl}/Auth/ResetPassword?token={token}";

            var html = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
                  <h2 style="color:#0d6efd">Сброс пароля — Общежитие ГГТУ</h2>
                  <p>Здравствуйте, <strong>{userResp.Data.FullName}</strong>!</p>
                  <p>Для установки нового пароля нажмите кнопку ниже:</p>
                  <p style="margin:24px 0">
                    <a href="{resetLink}"
                       style="background:#0d6efd;color:#fff;padding:12px 24px;
                              border-radius:6px;text-decoration:none;font-size:15px">
                      Сбросить пароль
                    </a>
                  </p>
                  <p style="color:#888;font-size:13px">Ссылка действительна 30 минут.<br>
                  Если вы не запрашивали сброс пароля — просто проигнорируйте это письмо.</p>
                </div>
                """;

            try
            {
                await _emailService.SendAsync(dto.Email.Trim(), "Сброс пароля — Общежитие ГГТУ", html);
            }
            catch (Exception ex)
            {
                // Возвращаем реальную причину, чтобы её можно было диагностировать
                return StatusCode(500, ApiResponse<bool>.Fail($"Ошибка отправки письма: {ex.Message}"));
            }

            return Ok(ApiResponse<bool>.Ok(true, "Письмо с инструкциями отправлено на указанный email"));
        }

        /// <summary>Сброс пароля по токену из письма</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest(ApiResponse<bool>.Fail("Токен не указан"));

            var email = _resetService.GetEmailByToken(dto.Token);
            if (email == null)
                return BadRequest(ApiResponse<bool>.Fail("Ссылка для сброса пароля недействительна или истекла. Запросите новую."));

            var resp = await _userService.ResetPasswordAsync(email, dto.NewPassword);
            if (resp.Success)
                _resetService.InvalidateToken(dto.Token);

            return HandleResponse(resp);
        }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
