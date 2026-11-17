using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Contracts;

public class ProductQuery
{
    public string? Search { get; set; }
    public ProductCategory? Category { get; set; }
    public ProductStage? Stage { get; set; }
    public Guid? OwnerId { get; set; }
    public bool FeaturedOnly { get; set; }
    public bool IncludeInactive { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;

    private int _pageSize = 12;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 60);
    }
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? StoreName { get; set; }
    public string? Bio { get; set; }
}

public class ProfileUpdate
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class ProductCreate
{
    public string Name { get; set; } = string.Empty;
    public ProductCategory Category { get; set; } = ProductCategory.Grains;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class AcquireRequest
{
    public Guid ProductId { get; set; }
    public Guid BuyerId { get; set; }
    public decimal NewPrice { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class CheckoutDetails
{
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash on delivery";
    public string Notes { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int pageSize = 12) => new() { Page = 1, PageSize = pageSize };
}

public record DataPoint(string Label, decimal Value);

public record CategorySlice(string Label, int Count);

public class DashboardSnapshot
{
    public decimal Revenue { get; init; }
    public decimal PendingValue { get; init; }
    public int Orders { get; init; }
    public int PendingOrders { get; init; }
    public int ActiveProducts { get; init; }
    public int Partners { get; init; }
    public int Customers { get; init; }
    public decimal InventoryValue { get; init; }
    public int CartItems { get; init; }
    public IReadOnlyList<DataPoint> RevenueSeries { get; init; } = Array.Empty<DataPoint>();
    public IReadOnlyList<CategorySlice> CategoryBreakdown { get; init; } = Array.Empty<CategorySlice>();
    public IReadOnlyList<Order> RecentOrders { get; init; } = Array.Empty<Order>();
    public IReadOnlyList<SupplyChainTransfer> RecentTransfers { get; init; } = Array.Empty<SupplyChainTransfer>();
}
