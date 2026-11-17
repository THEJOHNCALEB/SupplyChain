using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Entities;

/// <summary>
/// An immutable audit entry for a produce lot changing hands.
/// </summary>
public class SupplyChainTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? FromUserId { get; set; }
    public AppUser? FromUser { get; set; }

    public Guid ToUserId { get; set; }
    public AppUser? ToUser { get; set; }

    public ProductStage FromStage { get; set; }
    public ProductStage ToStage { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
