using System.Text.Json;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Transactions;

namespace DragonEnvelopes.Application.Tests;

public class ContractSerializationTests
{
    [Fact]
    public void FamilyResponse_RoundTripsWithMembers()
    {
        var dto = new FamilyResponse(
            Guid.NewGuid(),
            "Massey Household",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            [
                new FamilyMemberResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "keycloak-user-1",
                    "Terry",
                    "terry@example.com",
                    "Parent")
            ]);

        var json = JsonSerializer.Serialize(dto);
        var roundTrip = JsonSerializer.Deserialize<FamilyResponse>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal(dto.Name, roundTrip.Name);
        Assert.Single(roundTrip.Members);
        Assert.Equal("Parent", roundTrip.Members[0].Role);
    }

    [Fact]
    public void TransactionResponse_RoundTripsWithSplits()
    {
        var dto = new TransactionResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            120.42m,
            "Target purchase",
            "Target",
            new DateTimeOffset(2026, 3, 2, 13, 45, 0, TimeSpan.Zero),
            "Household",
            null,
            [
                new TransactionSplitResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 80m, "Groceries", null),
                new TransactionSplitResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40.42m, "Household", "Detergent")
            ]);

        var json = JsonSerializer.Serialize(dto);
        var roundTrip = JsonSerializer.Deserialize<TransactionResponse>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal(2, roundTrip.Splits.Count);
        Assert.Equal(120.42m, roundTrip.Amount);
    }

    [Fact]
    public void BudgetResponse_RoundTripsWithMoneyFields()
    {
        var dto = new BudgetResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026-03",
            5000m,
            4200m,
            800m);

        var json = JsonSerializer.Serialize(dto);
        var roundTrip = JsonSerializer.Deserialize<BudgetResponse>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal("2026-03", roundTrip.Month);
        Assert.Equal(800m, roundTrip.RemainingAmount);
    }
}

