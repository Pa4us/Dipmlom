using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SharedModel.DTOs;
using System.Security.Claims;
using WebAPP.Services;

namespace WebAPP.Controllers;

public class AuthController : Controller
{
    private readonly ApiClient _api;
    public AuthController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginDto model)
    {
        // Поле Login принимает как логин, так и email
        var response = await _api.PostAsync<LoginResponseDto>("api/auth/login", model);

        if (response?.Success != true || response.Data == null)
        {
            ModelState.AddModelError("", response?.Message ?? "Неверный логин или пароль");
            return View(model);
        }

        var user = response.Data.User;

        // Сохраняем JWT и данные пользователя в сессии
        HttpContext.Session.SetString("JWT", response.Data.Token);
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserFullName", user.FullName ?? user.Username);
        HttpContext.Session.SetString("Role", user.RoleName ?? "");

        // Создаём cookie-аутентификацию из JWT-claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName ?? user.Username),
            new(ClaimTypes.Role, user.RoleName ?? ""),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return user.RoleName switch
        {
            "Manager"   => RedirectToAction("Dashboard", "Manager"),
            "Educator"  => RedirectToAction("Dashboard", "Educator"),
            "Inspector" => RedirectToAction("Dashboard", "Inspector"),
            "Mechanic"  => RedirectToAction("Dashboard", "Mechanic"),
            _           => RedirectToAction("Dashboard", "Student"),
        };
    }

    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    // ─── Забыли пароль ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Введите email");
            return View();
        }

        var result = await _api.PostAsync<bool>("api/auth/forgot-password", new { Email = email.Trim() });

        if (result?.Success == true)
        {
            ViewBag.Sent = true;
        }
        else
        {
            // Показываем реальную ошибку (ошибка SMTP, соединения и т.п.)
            ModelState.AddModelError("", result?.Message ?? "Не удалось отправить письмо. Попробуйте позже.");
        }
        return View();
    }

    // ─── Сброс пароля по токену из письма ────────────────────────────────

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("ForgotPassword");
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        ViewBag.Token = token;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ModelState.AddModelError("", "Пароль должен содержать не менее 6 символов");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "Пароли не совпадают");
            return View();
        }

        var result = await _api.PostAsync<bool>("api/auth/reset-password",
            new { Token = token, NewPassword = newPassword, ConfirmPassword = confirmPassword });

        if (result?.Success == true)
        {
            TempData["ResetSuccess"] = "Пароль успешно изменён. Войдите с новым паролем.";
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", result?.Message ?? "Ссылка недействительна или истекла. Запросите новую.");
        return View();
    }
}
