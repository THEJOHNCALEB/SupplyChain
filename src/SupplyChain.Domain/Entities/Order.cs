using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Entities;

/// <summary>
/// A customer order plus the shipping snapshot taken at checkout time.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public AppUser? Customer { get; set; }

    public string Reference { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    public string PaymentMethod { get; set; } = "Cash on delivery";

    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
