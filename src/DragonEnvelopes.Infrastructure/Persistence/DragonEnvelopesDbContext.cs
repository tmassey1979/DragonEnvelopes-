using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Persistence;

public sealed class DragonEnvelopesDbContext(DbContextOptions<DragonEnvelopesDbContext> options) : DbContext(options)
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyFinancialProfile> FamilyFinancialProfiles => Set<FamilyFinancialProfile>();
    public DbSet<EnvelopeFinancialAccount> EnvelopeFinancialAccounts => Set<EnvelopeFinancialAccount>();
    public DbSet<EnvelopePaymentCard> EnvelopePaymentCards => Set<EnvelopePaymentCard>();
    public DbSet<EnvelopePaymentCardShipment> EnvelopePaymentCardShipments => Set<EnvelopePaymentCardShipment>();
    public DbSet<EnvelopePaymentCardControl> EnvelopePaymentCardControls => Set<EnvelopePaymentCardControl>();
    public DbSet<EnvelopePaymentCardControlAudit> EnvelopePaymentCardControlAudits => Set<EnvelopePaymentCardControlAudit>();
    public DbSet<EnvelopeRolloverRun> EnvelopeRolloverRuns => Set<EnvelopeRolloverRun>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();
    public DbSet<PlaidWebhookEvent> PlaidWebhookEvents => Set<PlaidWebhookEvent>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<SpendNotificationEvent> SpendNotificationEvents => Set<SpendNotificationEvent>();
    public DbSet<SpendAnomalyEvent> SpendAnomalyEvents => Set<SpendAnomalyEvent>();
    public DbSet<PlaidAccountLink> PlaidAccountLinks => Set<PlaidAccountLink>();
    public DbSet<PlaidSyncCursor> PlaidSyncCursors => Set<PlaidSyncCursor>();
    public DbSet<PlaidSyncedTransaction> PlaidSyncedTransactions => Set<PlaidSyncedTransaction>();
    public DbSet<PlaidBalanceSnapshot> PlaidBalanceSnapshots => Set<PlaidBalanceSnapshot>();
    public DbSet<OnboardingProfile> OnboardingProfiles => Set<OnboardingProfile>();
    public DbSet<FamilyInvite> FamilyInvites => Set<FamilyInvite>();
    public DbSet<FamilyInviteTimelineEvent> FamilyInviteTimelineEvents => Set<FamilyInviteTimelineEvent>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Envelope> Envelopes => Set<Envelope>();
    public DbSet<EnvelopeGoal> EnvelopeGoals => Set<EnvelopeGoal>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplitEntry> TransactionSplits => Set<TransactionSplitEntry>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecurringBill> RecurringBills => Set<RecurringBill>();
    public DbSet<RecurringBillExecution> RecurringBillExecutions => Set<RecurringBillExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DragonEnvelopesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
