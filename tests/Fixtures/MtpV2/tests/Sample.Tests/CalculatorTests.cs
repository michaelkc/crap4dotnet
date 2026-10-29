using Xunit;

namespace Sample.Tests;

public sealed class CalculatorTests
{
    [Theory]
    [InlineData(-3, 3)]
    [InlineData(3, 3)]
    public void Absolute(int value, int expected)
    {
        Assert.Equal(expected, Calculator.Absolute(value));
    }
}
