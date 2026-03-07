using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Tests;

public class EnvelopeRolloverCalculatorTests
{
    [Fact]
    public void Calculate_WithCapMode_RespectsCapAndRounding()
    {
        var result = EnvelopeRolloverCalculator.Calculate(123.456m, EnvelopeRolloverMode.Cap, 100.005m);

        Assert.Equal(100.01m, result);
    }

    [Fact]
    public void Calculate_WithNegativeBalance_ReturnsZero()
    {
        var result = EnvelopeRolloverCalculator.Calculate(-25m, EnvelopeRolloverMode.Full, rolloverCap: null);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void Calculate_WithNoneMode_ReturnsZero()
    {
        var result = EnvelopeRolloverCalculator.Calculate(55m, EnvelopeRolloverMode.None, rolloverCap: null);

        Assert.Equal(0m, result);
    }
}
