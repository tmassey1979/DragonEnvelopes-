using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Mapping;
using DragonEnvelopes.Application.Cqrs;
using DragonEnvelopes.Application.Cqrs.Accounts;
using DragonEnvelopes.Application.Cqrs.Imports;
using DragonEnvelopes.Application.Cqrs.Planning;
using DragonEnvelopes.Application.Cqrs.Transfers;
using DragonEnvelopes.Application.Cqrs.Transactions;
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
        services.AddScoped<IIntegrationOutboxDispatchService, IntegrationOutboxDispatchService>();
        services.AddScoped<IFamilyMemberImportService, FamilyMemberImportService>();
        services.AddScoped<IOnboardingProfileService, OnboardingProfileService>();
        services.AddScoped<IOnboardingBootstrapService, OnboardingBootstrapService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IFinancialIntegrationService, FinancialIntegrationService>();
        services.AddScoped<IPlaidTransactionSyncService, PlaidTransactionSyncService>();
        services.AddScoped<IPlaidBalanceReconciliationService, PlaidBalanceReconciliationService>();
        services.AddScoped<IEnvelopeFinancialAccountService, EnvelopeFinancialAccountService>();
        services.AddScoped<IEnvelopePaymentCardService, EnvelopePaymentCardService>();
        services.AddScoped<IEnvelopePaymentCardControlService, EnvelopePaymentCardControlService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IParentSpendNotificationService, ParentSpendNotificationService>();
        services.AddScoped<ISpendNotificationDispatchService, SpendNotificationDispatchService>();
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAutomationRuleService, AutomationRuleService>();
        services.AddScoped<ICategorizationRuleEngine, CategorizationRuleEngine>();
        services.AddScoped<IIncomeAllocationEngine, IncomeAllocationEngine>();
        services.AddScoped<IEnvelopeService, EnvelopeService>();
        services.AddScoped<IEnvelopeGoalService, EnvelopeGoalService>();
        services.AddScoped<IEnvelopeTransferService, EnvelopeTransferService>();
        services.AddScoped<IEnvelopeRolloverService, EnvelopeRolloverService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IRecurringBillService, RecurringBillService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ISpendAnomalyService, SpendAnomalyService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IScenarioSimulationService, ScenarioSimulationService>();
        services.AddScoped<ICommandHandler<CreateAccountCommand, DTOs.AccountDetails>, CreateAccountCommandHandler>();
        services.AddScoped<IQueryHandler<ListAccountsQuery, IReadOnlyList<DTOs.AccountDetails>>, ListAccountsQueryHandler>();
        services.AddScoped<IQueryHandler<PreviewTransactionImportQuery, DTOs.ImportPreviewDetails>, PreviewTransactionImportQueryHandler>();
        services.AddScoped<ICommandHandler<CommitTransactionImportCommand, DTOs.ImportCommitDetails>, CommitTransactionImportCommandHandler>();
        services.AddScoped<ICommandHandler<CreateEnvelopeTransferCommand, DTOs.EnvelopeTransferDetails>, CreateEnvelopeTransferCommandHandler>();
        services.AddScoped<ICommandHandler<CreateEnvelopeCommand, DTOs.EnvelopeDetails>, CreateEnvelopeCommandHandler>();
        services.AddScoped<IQueryHandler<GetEnvelopeByIdQuery, DTOs.EnvelopeDetails?>, GetEnvelopeByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListEnvelopesByFamilyQuery, IReadOnlyList<DTOs.EnvelopeDetails>>, ListEnvelopesByFamilyQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateEnvelopeCommand, DTOs.EnvelopeDetails>, UpdateEnvelopeCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateEnvelopeRolloverPolicyCommand, DTOs.EnvelopeDetails>, UpdateEnvelopeRolloverPolicyCommandHandler>();
        services.AddScoped<ICommandHandler<ArchiveEnvelopeCommand, DTOs.EnvelopeDetails>, ArchiveEnvelopeCommandHandler>();
        services.AddScoped<ICommandHandler<CreateBudgetCommand, DTOs.BudgetDetails>, CreateBudgetCommandHandler>();
        services.AddScoped<IQueryHandler<GetBudgetByMonthQuery, DTOs.BudgetDetails?>, GetBudgetByMonthQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateBudgetCommand, DTOs.BudgetDetails>, UpdateBudgetCommandHandler>();
        services.AddScoped<IQueryHandler<PreviewEnvelopeRolloverQuery, DTOs.EnvelopeRolloverPreviewDetails>, PreviewEnvelopeRolloverQueryHandler>();
        services.AddScoped<ICommandHandler<ApplyEnvelopeRolloverCommand, DTOs.EnvelopeRolloverApplyDetails>, ApplyEnvelopeRolloverCommandHandler>();
        services.AddScoped<ICommandHandler<CreateRecurringBillCommand, DTOs.RecurringBillDetails>, CreateRecurringBillCommandHandler>();
        services.AddScoped<IQueryHandler<ListRecurringBillsByFamilyQuery, IReadOnlyList<DTOs.RecurringBillDetails>>, ListRecurringBillsByFamilyQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateRecurringBillCommand, DTOs.RecurringBillDetails>, UpdateRecurringBillCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteRecurringBillCommand, bool>, DeleteRecurringBillCommandHandler>();
        services.AddScoped<IQueryHandler<ProjectRecurringBillsQuery, IReadOnlyList<DTOs.RecurringBillProjectionItemDetails>>, ProjectRecurringBillsQueryHandler>();
        services.AddScoped<IQueryHandler<ListRecurringBillExecutionsQuery, IReadOnlyList<DTOs.RecurringBillExecutionDetails>>, ListRecurringBillExecutionsQueryHandler>();
        services.AddScoped<ICommandHandler<CreateEnvelopeGoalCommand, DTOs.EnvelopeGoalDetails>, CreateEnvelopeGoalCommandHandler>();
        services.AddScoped<IQueryHandler<ListEnvelopeGoalsByFamilyQuery, IReadOnlyList<DTOs.EnvelopeGoalDetails>>, ListEnvelopeGoalsByFamilyQueryHandler>();
        services.AddScoped<IQueryHandler<GetEnvelopeGoalByIdQuery, DTOs.EnvelopeGoalDetails?>, GetEnvelopeGoalByIdQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateEnvelopeGoalCommand, DTOs.EnvelopeGoalDetails>, UpdateEnvelopeGoalCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteEnvelopeGoalCommand, bool>, DeleteEnvelopeGoalCommandHandler>();
        services.AddScoped<IQueryHandler<ProjectEnvelopeGoalsQuery, IReadOnlyList<DTOs.EnvelopeGoalProjectionDetails>>, ProjectEnvelopeGoalsQueryHandler>();
        services.AddScoped<ICommandHandler<CreateTransactionCommand, DTOs.TransactionDetails>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateTransactionCommand, DTOs.TransactionDetails>, UpdateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTransactionCommand, bool>, DeleteTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<RestoreTransactionCommand, DTOs.TransactionDetails>, RestoreTransactionCommandHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsByAccountQuery, IReadOnlyList<DTOs.TransactionDetails>>, ListTransactionsByAccountQueryHandler>();
        services.AddScoped<IQueryHandler<ListDeletedTransactionsQuery, IReadOnlyList<DTOs.TransactionDetails>>, ListDeletedTransactionsQueryHandler>();
        services.AddSingleton<IImportDedupService, ImportDedupService>();
        services.AddSingleton<IRemainingBudgetCalculator, RemainingBudgetCalculator>();
        services.AddSingleton<IApplicationMapper, IdentityMapper>();

        return services;
    }
}
