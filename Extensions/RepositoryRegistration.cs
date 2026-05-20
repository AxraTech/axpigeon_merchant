using AxpigeonApp.Repository;

namespace AxpigeonApp.Extensions
{
    public static class RepositoryRegistration
    {
        public static IServiceCollection AddAppRepositories(this IServiceCollection services)
        {
            // Register all repositories here
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMerchantRepository, MerchantRepository>();
            services.AddScoped<ITransactionsRepository, TransactionsRepository>();
            services.AddScoped<IKeyRepository, KeyRepository>();
            services.AddScoped<IHomeRepository, HomeRepository>();
            services.AddScoped<IScheduledSmsRepository, ScheduledSmsRepository>();

            return services;
        }
    }
}
