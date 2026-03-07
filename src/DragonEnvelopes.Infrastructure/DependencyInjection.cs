using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Infrastructure.Cqrs;
using DragonEnvelopes.Infrastructure.Messaging;
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
        var rabbitMqMessagingOptions = BuildRabbitMqMessagingOptions(configuration);
        services.AddSingleton(Options.Create(familyInviteEmailOptions));
        services.AddSingleton(Options.Create(providerSecretEncryptionOptions));
        services.AddSingleton(Options.Create(rabbitMqMessagingOptions));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IProviderSecretProtector, ProviderSecretProtector>();
        services.AddScoped<ICommandBus, InProcessCommandBus>();
        services.AddScoped<IQueryBus, InProcessQueryBus>();
        if (rabbitMqMessagingOptions.Enabled)
        {
            services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        }
        else
        {
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        }
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
        services.AddScoped<IEnvelopeGoalRepository, EnvelopeGoalRepository>();
        services.AddScoped<IEnvelopeRolloverRunRepository, EnvelopeRolloverRunRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IRecurringBillRepository, RecurringBillRepository>();
        services.AddScoped<IRecurringBillExecutionRepository, RecurringBillExecutionRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();

        return services;
    }

    private static RabbitMqMessagingOptions BuildRabbitMqMessagingOptions(IConfiguration configuration)
    {
        var enabled = bool.TryParse(configuration["Messaging:RabbitMq:Enabled"], out var enabledValue) && enabledValue;
        var hostName = configuration["Messaging:RabbitMq:HostName"] ?? "localhost";
        var port = int.TryParse(configuration["Messaging:RabbitMq:Port"], out var portValue)
            ? portValue
            : 5672;
        var userName = configuration["Messaging:RabbitMq:UserName"] ?? "guest";
        var password = configuration["Messaging:RabbitMq:Password"] ?? "guest";
        var virtualHost = configuration["Messaging:RabbitMq:VirtualHost"] ?? "/";
        var exchangeName = configuration["Messaging:RabbitMq:ExchangeName"] ?? "dragonenvelopes.events";
        var exchangeType = configuration["Messaging:RabbitMq:ExchangeType"] ?? "topic";
        var durableExchange = !bool.TryParse(configuration["Messaging:RabbitMq:DurableExchange"], out var durableValue)
            || durableValue;
        var enableLedgerConsumer = !bool.TryParse(configuration["Messaging:RabbitMq:EnableLedgerTransactionConsumer"], out var consumerValue)
            || consumerValue;
        var ledgerQueue = configuration["Messaging:RabbitMq:LedgerTransactionCreatedQueue"]
            ?? "dragonenvelopes.financial.ledger-transaction-created";
        var prefetchCount = ushort.TryParse(configuration["Messaging:RabbitMq:ConsumerPrefetchCount"], out var prefetchValue)
            ? prefetchValue
            : (ushort)20;

        return new RabbitMqMessagingOptions
        {
            Enabled = enabled,
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            VirtualHost = virtualHost,
            ExchangeName = exchangeName,
            ExchangeType = exchangeType,
            DurableExchange = durableExchange,
            EnableLedgerTransactionConsumer = enableLedgerConsumer,
            LedgerTransactionCreatedQueue = ledgerQueue,
            ConsumerPrefetchCount = prefetchCount
        };
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
