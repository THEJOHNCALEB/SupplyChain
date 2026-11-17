using System.ComponentModel.DataAnnotations;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;

namespace SupplyChain.Web.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Product> Featured { get; init; } = Array.Empty<Product>();
    public IReadOnlyDictionary<ProductCategory, int> CategoryCounts { get; init; } =
        new Dictionary<ProductCategory, int>();
    public int ProductCount { get; init; }
    public int PartnerCount { get; init; }
    public int CustomerCount { get; init; }
    public int DeliveredOrders { get; init; }
}

public class MarketplaceViewModel
{
    public PagedResult<Product> Results { get; init; } = PagedResult<Product>.Empty();
    public ProductQuery Query { get; init; } = new();
    public IReadOnlyDictionary<ProductCategory, int> CategoryCounts { get; init; } =
        new Dictionary<ProductCategory, int>();
    public UserRole? ViewerRole { get; init; }
    public ProductStage Stage { get; init; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; init; } = new();
    public IReadOnlyList<SupplyChainTransfer> Transfers { get; init; } = Array.Empty<SupplyChainTransfer>();
    public IReadOnlyList<Product> Related { get; init; } = Array.Empty<Product>();
    public bool CanAcquire { get; init; }
    public bool CanBuy { get; init; }
    public bool IsOwner { get; init; }
}

public class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(160, MinimumLength = 3)]
    [Display(Name = "Lot name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ProductCategory Category { get; set; } = ProductCategory.Grains;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Unit { get; set; } = "kg";

    [Range(1, 100_000_000, ErrorMessage = "Enter a price greater than zero.")]
    public decimal Price { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Enter a quantity greater than zero.")]
    public int Quantity { get; set; }

    [StringLength(120)]
    public string Origin { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }
    public string? ExistingImageUrl { get; set; }
}

public class CartViewModel
{
    public IReadOnlyList<CartItem> Items { get; init; } = Array.Empty<CartItem>();
    public decimal Subtotal { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }
}

public class CheckoutViewModel
{
    public IReadOnlyList<CartItem> Items { get; init; } = Array.Empty<CartItem>();
    public decimal Subtotal { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }

    [Required(ErrorMessage = "Who should we deliver to?")]
    [Display(Name = "Full name")]
    public string ShippingName { get; set; } = string.Empty;

    [Required, Phone]
    [Display(Name = "Phone number")]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Delivery address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    public string ShippingState { get; set; } = string.Empty;

    [Required]
    public string PaymentMethod { get; set; } = "Cash on delivery";

    public string? Notes { get; set; }
}

public class DashboardViewModel
{
    public required AppUser User { get; init; }
    public required DashboardSnapshot Snapshot { get; init; }
    public IReadOnlyList<Product> Products { get; init; } = Array.Empty<Product>();
}

public class SalesViewModel
{
    public required AppUser User { get; init; }
    public IReadOnlyList<Order> Orders { get; init; } = Array.Empty<Order>();
    public IReadOnlyList<SupplyChainTransfer> Deals { get; init; } = Array.Empty<SupplyChainTransfer>();
    public bool ShowOrders { get; init; }
    public bool ShowDeals { get; init; }
}

public class OrdersViewModel
{
    public required AppUser User { get; init; }
    public IReadOnlyList<Order> Orders { get; init; } = Array.Empty<Order>();
    public bool IsSellerView { get; init; }
}

public class AdminUsersViewModel
{
    public IReadOnlyList<AppUser> Users { get; init; } = Array.Empty<AppUser>();
    public string? Search { get; init; }
    public UserRole? Role { get; init; }
    public int Farmers { get; init; }
    public int Distributors { get; init; }
    public int Retailers { get; init; }
    public int Customers { get; init; }
    public int Admins { get; init; }
}

public class ProfilePageViewModel
{
    public required AppUser User { get; init; }
    public ProfileViewModel Profile { get; init; } = new();
    public ChangePasswordViewModel Password { get; init; } = new();
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "We hit an unexpected snag. Please try again.";
}
