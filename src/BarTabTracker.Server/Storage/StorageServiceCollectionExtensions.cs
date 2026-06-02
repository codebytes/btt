using BarTabTracker.Server.Storage.Redis;

namespace BarTabTracker.Server.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBarTabStorage(this IServiceCollection services)
    {
        // TODO: Enable Redis AOF/RDB persistence before relying on Redis for durable tab history.
        services.AddScoped<IUserRepository, RedisUserRepository>();
        services.AddScoped<ITabRepository, RedisTabRepository>();

        return services;
    }
}
