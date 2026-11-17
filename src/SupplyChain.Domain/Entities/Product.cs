using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Entities;

/// <summary>
/// A lot of produce moving through the supply chain. The same row changes hands
/// (and price) as distributors and retailers acquire it, while every hand-off is
/// captured in <see cref="SupplyChainTransfer"/>.
/// </summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public ProductCategory Category { get; set; } = ProductCategory.Other;
    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = "kg";
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public int InitialQuantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;

    public ProductStage Stage { get; set; } = ProductStage.Farm;

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplyChainTransfer> Transfers { get; set; } = new List<SupplyChainTransfer>();

    /// <summary>
    /// The role the product is waiting for next. Null once sold.
    /// </summary>
    public UserRole? NextRole => Stage switch
    {
        ProductStage.Farm => UserRole.Distributor,
        ProductStage.Distribution => UserRole.Retailer,
        ProductStage.Retail => UserRole.Customer,
        _ => null
    };

    public decimal Margin => Price - CostPrice;

    public decimal MarginPercent => CostPrice <= 0 ? 0 : Math.Round(Margin / CostPrice * 100m, 1);
}
