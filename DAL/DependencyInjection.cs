using DAL.DBContext;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }

        /// <summary>
        /// Только репозитории, без AppDbContext. Используется в тестовом окружении,
        /// где DbContext поднимается тест-фабрикой поверх InMemory-провайдера.
        /// </summary>
        public static IServiceCollection AddDataAccessRepositoriesOnly(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            return services;
        }
    }
}
