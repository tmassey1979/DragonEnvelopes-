using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Tests;

public class RemainingBudgetCalculatorTests
{
    private readonly RemainingBudgetCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsZeroRemaining_WhenIncomeAndAllocationsAreZero()
    {
        var result = _calculator.Calculate(0m, [0m, 0m]);

        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.AllocatedAmount);
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Fact]
    public void Calculate_ReturnsNegativeRemaining_WhenOverBudget()
    {
        var result = _calculator.Calculate(100m, [50m, 80m]);

        Assert.Equal(100m, result.TotalIncome);
        Assert.Equal(130m, result.AllocatedAmount);
        Assert.Equal(-30m, result.RemainingAmount);
    }

    [Fact]
    public void Calculate_IgnoresNegativeIncomeAndAllocationValues()
    {
        var result = _calculator.Calculate(-100m, [-10m, 25m, -1m]);

        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(25m, result.AllocatedAmount);
        Assert.Equal(-25m, result.RemainingAmount);
    }
}
