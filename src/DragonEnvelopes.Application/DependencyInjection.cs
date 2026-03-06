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
        services.AddScoped<IFamilyInviteService, FamilyInviteService>();
        services.AddScoped<IOnboardingProfileService, OnboardingProfileService>();
        services.AddScoped<IOnboardingBootstrapService, OnboardingBootstrapService>();
        services.AddScoped<IFinancialIntegrationService, FinancialIntegrationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAutomationRuleService, AutomationRuleService>();
        services.AddScoped<ICategorizationRuleEngine, CategorizationRuleEngine>();
        services.AddScoped<IIncomeAllocationEngine, IncomeAllocationEngine>();
        services.AddScoped<IEnvelopeService, EnvelopeService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRecurringBillService, RecurringBillService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddSingleton<IImportDedupService, ImportDedupService>();
        services.AddSingleton<IRemainingBudgetCalculator, RemainingBudgetCalculator>();
        services.AddSingleton<IApplicationMapper, IdentityMapper>();

        return services;
    }
}
