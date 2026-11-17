namespace SupplyChain.Domain.Enums;

public static class EnumDisplay
{
    public static string ToDisplay(this UserRole role) => role switch
    {
        UserRole.Farmer => "Farmer",
        UserRole.Distributor => "Distributor",
        UserRole.Retailer => "Retailer",
        UserRole.Customer => "Customer",
        UserRole.Admin => "Admin",
        _ => role.ToString()
    };

    public static string ToDisplay(this ProductStage stage) => stage switch
    {
        ProductStage.Farm => "At the farm",
        ProductStage.Distribution => "In distribution",
        ProductStage.Retail => "In retail",
        ProductStage.Sold => "Sold",
        _ => stage.ToString()
    };

    public static string ToDisplay(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Awaiting payment",
        OrderStatus.Paid => "Paid",
        OrderStatus.Processing => "Processing",
        OrderStatus.Shipped => "On the way",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string ToDisplay(this ProductCategory category) => category switch
    {
        ProductCategory.Grains => "Grains",
        ProductCategory.Tubers => "Tubers",
        ProductCategory.Fruits => "Fruits",
        ProductCategory.Vegetables => "Vegetables",
        ProductCategory.Livestock => "Livestock",
        ProductCategory.Dairy => "Dairy",
        ProductCategory.Spices => "Spices",
        _ => "Other"
    };

    public static string Icon(this ProductCategory category) => category switch
    {
        ProductCategory.Grains => "grain",
        ProductCategory.Tubers => "tuber",
        ProductCategory.Fruits => "fruit",
        ProductCategory.Vegetables => "leaf",
        ProductCategory.Livestock => "livestock",
        ProductCategory.Dairy => "droplet",
        ProductCategory.Spices => "flame",
        _ => "basket"
    };

    public static string BadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "badge-warn",
        OrderStatus.Paid => "badge-info",
        OrderStatus.Processing => "badge-info",
        OrderStatus.Shipped => "badge-info",
        OrderStatus.Delivered => "badge-success",
        OrderStatus.Cancelled => "badge-danger",
        _ => "badge-muted"
    };

    /// <summary>
    /// The role that can act on a lot at a given stage.
    /// </summary>
    public static UserRole? NextActor(this ProductStage stage) => stage switch
    {
        ProductStage.Farm => UserRole.Distributor,
        ProductStage.Distribution => UserRole.Retailer,
        ProductStage.Retail => UserRole.Customer,
        _ => null
    };
}

/// <summary>
/// Presentation-agnostic catalog that describes each role and its place in the chain.
/// </summary>
public static class RoleCatalog
{
    public static readonly UserRole[] ChainOrder =
    {
        UserRole.Farmer,
        UserRole.Distributor,
        UserRole.Retailer,
        UserRole.Customer
    };

    public static string Tagline(UserRole role) => role switch
    {
        UserRole.Farmer => "List harvests and reach buyers beyond your local market.",
        UserRole.Distributor => "Source directly from farms and move volume with confidence.",
        UserRole.Retailer => "Stock your shelves from verified distributors.",
        UserRole.Customer => "Buy fresh, traceable produce from trusted local sellers.",
        UserRole.Admin => "Keep the marketplace healthy, trusted and growing.",
        _ => string.Empty
    };

    public static string StageLabel(UserRole role) => role switch
    {
        UserRole.Farmer => "Grow",
        UserRole.Distributor => "Move",
        UserRole.Retailer => "Sell",
        UserRole.Customer => "Enjoy",
        UserRole.Admin => "Operate",
        _ => string.Empty
    };

    public static ProductStage StageFor(UserRole role) => role switch
    {
        UserRole.Farmer => ProductStage.Farm,
        UserRole.Distributor => ProductStage.Distribution,
        UserRole.Retailer => ProductStage.Retail,
        _ => ProductStage.Sold
    };
}
