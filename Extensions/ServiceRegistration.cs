using AxpigeonApp.Service;
using AxpigeonApp.Services;

namespace AxpigeonApp.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            // Register all services here
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMerchantService, MerchantService>();
            services.AddScoped<ITransactionsService, TransactionsService>();
            services.AddScoped<IKeyService, KeyService>();
            services.AddScoped<IHomeService, HomeService>();
            services.AddScoped<IScheduledSmsService, ScheduledSmsService>();

            return services;
        }
    }
}
