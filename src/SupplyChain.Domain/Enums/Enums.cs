namespace SupplyChain.Domain.Enums;

/// <summary>
/// The actors in the agricultural supply chain, from producer to end consumer.
/// </summary>
public enum UserRole
{
    Farmer = 1,
    Distributor = 2,
    Retailer = 3,
    Customer = 4,
    Admin = 5
}

/// <summary>
/// Where a lot currently sits in the farm-to-doorstep journey.
/// </summary>
public enum ProductStage
{
    Farm = 1,
    Distribution = 2,
    Retail = 3,
    Sold = 4
}

/// <summary>
/// Lifecycle of a customer order.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

/// <summary>
/// Top-level produce taxonomy used for discovery and filtering.
/// </summary>
public enum ProductCategory
{
    Grains = 1,
    Tubers = 2,
    Fruits = 3,
    Vegetables = 4,
    Livestock = 5,
    Dairy = 6,
    Spices = 7,
    Other = 8
}
