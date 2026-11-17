using SupplyChain.Domain.Contracts;

namespace SupplyChain.Tests;

public class PricingRulesTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-100, 0)]
    [InlineData(1, 2_500)]
    [InlineData(99_999, 2_500)]
    [InlineData(100_000, 0)]
    [InlineData(250_000, 0)]
    public void DeliveryFor_AppliesTheThresholdCorrectly(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, PricingRules.DeliveryFor(subtotal));
    }

    [Theory]
    [InlineData(10_000, 12_500)]
    [InlineData(100_000, 100_000)]
    public void GrandTotal_AddsDeliveryOnlyWhenNeeded(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, PricingRules.GrandTotal(subtotal));
    }
}
