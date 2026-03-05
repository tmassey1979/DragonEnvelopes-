using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Mapping;
using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthPingService, HealthPingService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEnvelopeService, EnvelopeService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddSingleton<IApplicationMapper, IdentityMapper>();

        return services;
    }
}
