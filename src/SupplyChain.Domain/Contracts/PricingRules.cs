namespace SupplyChain.Domain.Contracts;

/// <summary>
/// Central place for money rules so the cart, checkout, receipts and tests all
/// agree on the same numbers.
/// </summary>
public static class PricingRules
{
    public const decimal DeliveryFee = 2_500m;
    public const decimal FreeDeliveryThreshold = 100_000m;

    public static decimal DeliveryFor(decimal subtotal) => subtotal switch
    {
        <= 0 => 0,
        >= FreeDeliveryThreshold => 0,
        _ => DeliveryFee
    };

    public static decimal GrandTotal(decimal subtotal) => subtotal + DeliveryFor(subtotal);
}
