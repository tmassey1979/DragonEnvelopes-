using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Infrastructure.Repositories;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
        }

        services.AddDbContext<DragonEnvelopesDbContext>(options => options.UseNpgsql(connectionString));
        var familyInviteEmailOptions = new FamilyInviteEmailOptions
        {
            Enabled = bool.TryParse(configuration["FamilyInvites:Email:Enabled"], out var enabled) && enabled,
            UseSmtp = bool.TryParse(configuration["FamilyInvites:Email:UseSmtp"], out var useSmtp) && useSmtp,
            InviteBaseUrl = configuration["FamilyInvites:Email:InviteBaseUrl"] ?? "http://localhost:5173",
            FromAddress = configuration["FamilyInvites:Email:FromAddress"] ?? "noreply@dragonenvelopes.local",
            SmtpHost = configuration["FamilyInvites:Email:SmtpHost"] ?? string.Empty,
            SmtpPort = int.TryParse(configuration["FamilyInvites:Email:SmtpPort"], out var smtpPort) ? smtpPort : 25,
            SmtpUsername = configuration["FamilyInvites:Email:SmtpUsername"],
            SmtpPassword = configuration["FamilyInvites:Email:SmtpPassword"],
            SmtpEnableSsl = bool.TryParse(configuration["FamilyInvites:Email:SmtpEnableSsl"], out var smtpEnableSsl) && smtpEnableSsl
        };
        var providerSecretEncryptionOptions = BuildProviderSecretEncryptionOptions(configuration);
        services.AddSingleton(Options.Create(familyInviteEmailOptions));
        services.AddSingleton(Options.Create(providerSecretEncryptionOptions));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IProviderSecretProtector, ProviderSecretProtector>();
        services.AddScoped<IFamilyInviteSender, FamilyInviteSender>();
        services.AddScoped<IRepositoryMarker, RepositoryMarker>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyInviteRepository, FamilyInviteRepository>();
        services.AddScoped<IFamilyFinancialProfileRepository, FamilyFinancialProfileRepository>();
        services.AddScoped<IEnvelopeFinancialAccountRepository, EnvelopeFinancialAccountRepository>();
        services.AddScoped<IEnvelopePaymentCardRepository, EnvelopePaymentCardRepository>();
        services.AddScoped<IEnvelopePaymentCardShipmentRepository, EnvelopePaymentCardShipmentRepository>();
        services.AddScoped<IEnvelopePaymentCardControlRepository, EnvelopePaymentCardControlRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<ISpendNotificationEventRepository, SpendNotificationEventRepository>();
        services.AddScoped<IPlaidAccountLinkRepository, PlaidAccountLinkRepository>();
        services.AddScoped<IPlaidSyncCursorRepository, PlaidSyncCursorRepository>();
        services.AddScoped<IPlaidSyncedTransactionRepository, PlaidSyncedTransactionRepository>();
        services.AddScoped<IPlaidBalanceSnapshotRepository, PlaidBalanceSnapshotRepository>();
        services.AddScoped<IOnboardingProfileRepository, OnboardingProfileRepository>();
        services.AddScoped<IOnboardingBootstrapRepository, OnboardingBootstrapRepository>();
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IEnvelopeRepository, EnvelopeRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IRecurringBillRepository, RecurringBillRepository>();
        services.AddScoped<IRecurringBillExecutionRepository, RecurringBillExecutionRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();

        return services;
    }

    private static ProviderSecretEncryptionOptions BuildProviderSecretEncryptionOptions(IConfiguration configuration)
    {
        var keys = configuration
            .GetSection("ProviderSecretEncryption:Keys")
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key,
                static child => child.Value ?? string.Empty,
                StringComparer.Ordinal);

        var activeKeyId = configuration["ProviderSecretEncryption:ActiveKeyId"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(activeKeyId) && keys.Count > 0)
        {
            activeKeyId = keys.Keys.First();
        }

        return new ProviderSecretEncryptionOptions
        {
            Enabled = bool.TryParse(configuration["ProviderSecretEncryption:Enabled"], out var enabled) && enabled,
            ActiveKeyId = activeKeyId,
            Keys = keys
        };
    }
}
