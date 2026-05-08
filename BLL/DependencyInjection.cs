using AutoMapper;
using BLL.Interfaces;
using BLL.Mapping;
using BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
        {
            // AutoMapper - регистрация всех профилей в текущей сборке
            services.AddAutoMapper(cfg => {
                cfg.AddProfile<MappingProfile>();
            });

            // Сервисы
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IBlockService, BlockService>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IResidenceService, ResidenceService>();
            services.AddScoped<IRepairRequestService, RepairRequestService>();
            services.AddScoped<IInspectionService, InspectionService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IStudentPointService, StudentPointService>();
            services.AddScoped<IStatisticsService, StatisticsService>();

            return services;
        }
    }
}
