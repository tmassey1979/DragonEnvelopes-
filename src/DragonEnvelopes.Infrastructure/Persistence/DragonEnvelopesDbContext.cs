using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Persistence;

public sealed class DragonEnvelopesDbContext(DbContextOptions<DragonEnvelopesDbContext> options) : DbContext(options)
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyInvite> FamilyInvites => Set<FamilyInvite>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Envelope> Envelopes => Set<Envelope>();
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
