using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Infrastructure.Repositories;
using DragonEnvelopes.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IRepositoryMarker, RepositoryMarker>();

        return services;
    }
}

