using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class Budget
{
    private readonly Dictionary<Guid, Money> _allocations = [];

    public Guid Id { get; }
    public Guid FamilyId { get; }
    public BudgetMonth Month { get; }
    public Money TotalIncome { get; private set; }
    public Money AllocatedAmount => _allocations.Values.Aggregate(Money.Zero, static (sum, value) => sum + value);
    public Money RemainingAmount => TotalIncome - AllocatedAmount;
    public IReadOnlyDictionary<Guid, Money> Allocations => _allocations;

    public Budget(Guid id, Guid familyId, BudgetMonth month, Money totalIncome)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Budget id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        Month = month;
        TotalIncome = totalIncome.EnsureNonNegative("Total income");
    }

    public void SetTotalIncome(Money totalIncome)
    {
        totalIncome.EnsureNonNegative("Total income");
        if (totalIncome < AllocatedAmount)
        {
            throw new DomainValidationException("Total income cannot be lower than allocated amount.");
        }

        TotalIncome = totalIncome;
    }

    public void SetAllocation(Guid envelopeId, Money amount)
    {
        if (envelopeId == Guid.Empty)
        {
            throw new DomainValidationException("Envelope id is required.");
        }

        amount.EnsureNonNegative("Allocation amount");

        var current = _allocations.TryGetValue(envelopeId, out var existing)
            ? existing
            : Money.Zero;

        var nextAllocated = AllocatedAmount - current + amount;
        if (nextAllocated > TotalIncome)
        {
            throw new DomainValidationException("Total allocations cannot exceed total income.");
        }

        _allocations[envelopeId] = amount;
    }

    public void RemoveAllocation(Guid envelopeId)
    {
        _allocations.Remove(envelopeId);
    }
}

