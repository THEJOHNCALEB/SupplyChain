namespace SupplyChain.Domain.Entities;

/// <summary>
/// A frozen snapshot of what was purchased. Historical orders must never change
/// when a product is edited or delisted.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public Guid SellerId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public string ImageUrl { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
