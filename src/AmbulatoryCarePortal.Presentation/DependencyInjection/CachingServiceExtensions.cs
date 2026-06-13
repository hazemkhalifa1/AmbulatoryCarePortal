using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration.GetValue<string>("Redis:ConnectionString")
            ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (string.IsNullOrEmpty(redisConn))
        {
            services.AddDistributedMemoryCache();
            return services;
        }

        try
        {
            using var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { redisConn },
                AbortOnConnectFail = false,
                ConnectTimeout = 2000,
                ConnectRetry = 0
            });

            if (redis.IsConnected)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                    options.InstanceName = "CBAHI_";
                });
                return services;
            }
        }
        catch
        {
        }

        services.AddDistributedMemoryCache();
        return services;
    }
}
