using BLL;
using BLL.Interfaces;
using DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;
using WebAPI.Infrastructure;
using WebAPI.Services;

namespace WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── DAL: DbContext + Repositories ──────────────────────────────
            builder.Services.AddDataAccess(builder.Configuration);

            // ── BLL: Сервисы + AutoMapper ───────────────────────────────────
            builder.Services.AddBusinessLogic();

            // ── Email + Password Reset ───────────────────────────────────────
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddSingleton<IPasswordResetService, PasswordResetService>();

            // ── JWT ─────────────────────────────────────────────────────────
            builder.Services.AddScoped<IJwtService, JwtService>();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"]!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

            builder.Services.AddAuthorization();

            // ── Controllers + OpenAPI ────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // ── Swagger UI с поддержкой JWT Bearer ──────────────────────────
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Общежитие ГГТУ API",
                    Version = "v1",
                    Description = "API для управления общежитием ГГТУ им. П.О. Сухого"
                });

                // Схема авторизации Bearer
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "Bearer",
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Description  = "Введите JWT токен. Пример: Bearer eyJhbG..."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ── CORS (для фронта на WebAPP) ─────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebAPP", policy =>
                    policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                // Swagger UI: /swagger
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Общежитие ГГТУ v1");
                    c.RoutePrefix = "swagger";
                });

                // Scalar UI (альтернатива): /scalar/v1
                app.MapScalarApiReference();
            }

            // UseHttpsRedirection убран: WebAPP обращается к API через HttpClient
            // на http://localhost:5229. Редирект на HTTPS сбрасывает Authorization-заголовок
            // (cross-origin redirect) → 401 на все защищённые эндпоинты.
            app.UseCors("AllowWebAPP");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ── Seed данные при первом запуске в Development ────────────────
            if (app.Environment.IsDevelopment())
            {
                await DbSeeder.SeedAsync(app.Services);
            }

            app.Run();
        }
    }
}
