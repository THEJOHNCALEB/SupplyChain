using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly SupplyChainDbContext _db;

    public DashboardService(SupplyChainDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> GetAsync(AppUser user, CancellationToken ct = default)
    {
        return user.Role switch
        {
            UserRole.Admin => await AdminSnapshotAsync(ct),
            UserRole.Customer => await CustomerSnapshotAsync(user.Id, ct),
            _ => await SellerSnapshotAsync(user, ct)
        };
    }

    private async Task<DashboardSnapshot> SellerSnapshotAsync(AppUser user, CancellationToken ct)
    {
        var userId = user.Id;
        var products = await _db.Products
            .AsNoTracking()
            .Where(x => x.OwnerId == userId)
            .ToListAsync(ct);

        if (user.Role is UserRole.Farmer or UserRole.Distributor)
        {
            var deals = await _db.Transfers
                .AsNoTracking()
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Where(x => x.FromUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);

            var dealLines = deals
                .Select(x => new DatedAmount(x.CreatedAt, x.UnitPrice * x.Quantity))
                .ToList();

            return new DashboardSnapshot
            {
                Revenue = dealLines.Sum(x => x.Amount),
                PendingValue = 0,
                Orders = deals.Count,
                PendingOrders = 0,
                ActiveProducts = products.Count(x => x.IsActive && x.Quantity > 0),
                InventoryValue = products.Where(x => x.IsActive).Sum(x => x.Quantity * x.Price),
                Partners = deals.Where(x => x.ToUserId != Guid.Empty).Select(x => x.ToUserId).Distinct().Count(),
                Customers = deals.Select(x => x.ToUserId).Distinct().Count(),
                RevenueSeries = BuildSeries(dealLines),
                CategoryBreakdown = products
                    .GroupBy(x => x.Category)
                    .Select(g => new CategorySlice(g.Key.ToDisplay(), g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                RecentTransfers = deals.Take(5).ToList()
            };
        }

        var sales = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Customer)
            .Where(x => x.Status != OrderStatus.Cancelled && x.Items.Any(i => i.SellerId == userId))
            .ToListAsync(ct);

        var lines = sales
            .SelectMany(order => order.Items
                .Where(item => item.SellerId == userId)
                .Select(item => new DatedAmount(order.CreatedAt, item.LineTotal)))
            .ToList();

        var pending = sales
            .Where(x => x.Status is OrderStatus.Pending or OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped)
            .ToList();

        return new DashboardSnapshot
        {
            Revenue = lines.Sum(x => x.Amount),
            PendingValue = pending
                .SelectMany(o => o.Items.Where(i => i.SellerId == userId))
                .Sum(i => i.LineTotal),
            Orders = sales.Count,
            PendingOrders = pending.Count,
            ActiveProducts = products.Count(x => x.IsActive && x.Quantity > 0),
            InventoryValue = products.Where(x => x.IsActive).Sum(x => x.Quantity * x.Price),
            Partners = sales.Select(x => x.CustomerId).Distinct().Count(),
            Customers = sales.Select(x => x.CustomerId).Distinct().Count(),
            RevenueSeries = BuildSeries(lines),
            CategoryBreakdown = products
                .GroupBy(x => x.Category)
                .Select(g => new CategorySlice(g.Key.ToDisplay(), g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList(),
            RecentOrders = sales.OrderByDescending(x => x.CreatedAt).Take(5).ToList()
        };
    }

    private async Task<DashboardSnapshot> CustomerSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var cart = await _db.CartItems
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Quantity, x.UnitPrice })
            .ToListAsync(ct);

        var retailCount = await _db.Products
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.Stage == ProductStage.Retail && x.Quantity > 0, ct);

        var lines = orders
            .SelectMany(order => order.Items.Select(item => new DatedAmount(order.CreatedAt, item.LineTotal)))
            .ToList();

        var productIds = orders
            .SelectMany(x => x.Items)
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(ct);

        var purchased = orders.SelectMany(x => x.Items).ToList();
        var categories = purchased
            .Join(products, item => item.ProductId, product => product.Id, (item, product) => product.Category)
            .GroupBy(x => x)
            .Select(g => new CategorySlice(g.Key.ToDisplay(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new DashboardSnapshot
        {
            Revenue = orders.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.Total),
            PendingValue = orders
                .Where(x => x.Status is OrderStatus.Pending or OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped)
                .Sum(x => x.Total),
            Orders = orders.Count,
            PendingOrders = orders.Count(x => x.Status is OrderStatus.Pending or OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped),
            ActiveProducts = retailCount,
            InventoryValue = cart.Sum(x => x.Quantity * x.UnitPrice),
            CartItems = cart.Sum(x => x.Quantity),
            Partners = purchased.Select(x => x.SellerId).Distinct().Count(),
            RevenueSeries = BuildSeries(lines),
            CategoryBreakdown = categories,
            RecentOrders = orders.Take(5).ToList()
        };
    }

    private async Task<DashboardSnapshot> AdminSnapshotAsync(CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Customer)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var products = await _db.Products.AsNoTracking().ToListAsync(ct);
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);
        var cart = await _db.CartItems.AsNoTracking().Select(x => new { x.Quantity, x.UnitPrice }).ToListAsync(ct);

        var validOrders = orders.Where(x => x.Status != OrderStatus.Cancelled).ToList();
        var lines = validOrders
            .SelectMany(x => x.Items.Select(item => new DatedAmount(x.CreatedAt, item.LineTotal)))
            .ToList();

        var pending = orders
            .Where(x => x.Status is OrderStatus.Pending or OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped)
            .ToList();

        return new DashboardSnapshot
        {
            Revenue = validOrders.Sum(x => x.Total),
            PendingValue = pending.Sum(x => x.Total),
            Orders = orders.Count,
            PendingOrders = pending.Count,
            ActiveProducts = products.Count(x => x.IsActive && x.Quantity > 0),
            InventoryValue = products.Where(x => x.IsActive).Sum(x => x.Quantity * x.Price),
            Partners = users.Count(x => x.Role is UserRole.Farmer or UserRole.Distributor or UserRole.Retailer),
            Customers = users.Count(x => x.Role == UserRole.Customer),
            CartItems = cart.Sum(x => x.Quantity),
            RevenueSeries = BuildSeries(lines),
            CategoryBreakdown = products
                .GroupBy(x => x.Category)
                .Select(g => new CategorySlice(g.Key.ToDisplay(), g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList(),
            RecentOrders = orders.Take(5).ToList()
        };
    }

    private static IReadOnlyList<DataPoint> BuildSeries(IEnumerable<DatedAmount> entries)
    {
        var list = entries.ToList();
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-5);

        return Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var month = start.AddMonths(offset);
                var total = list
                    .Where(x => x.At.Year == month.Year && x.At.Month == month.Month)
                    .Sum(x => x.Amount);
                return new DataPoint(month.ToString("MMM"), total);
            })
            .ToList();
    }

    private readonly record struct DatedAmount(DateTime At, decimal Amount);
}
