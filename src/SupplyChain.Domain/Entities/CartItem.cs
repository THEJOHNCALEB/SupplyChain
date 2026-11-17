namespace SupplyChain.Domain.Entities;

/// <summary>
/// A line in a customer's cart. Price is captured when added so the cart is
/// stable while browsing, but is re-validated at checkout.
/// </summary>
public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public decimal LineTotal => UnitPrice * Quantity;
}
