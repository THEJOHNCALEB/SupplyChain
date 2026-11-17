using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Common;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly SupplyChainDbContext _db;

    public OrderService(SupplyChainDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<Order>> CheckoutAsync(
        Guid userId,
        CheckoutDetails details,
        CancellationToken ct = default)
    {
        var customer = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (customer is null)
        {
            return ServiceResult<Order>.Fail("Account not found.");
        }

        if (string.IsNullOrWhiteSpace(details.ShippingName) ||
            string.IsNullOrWhiteSpace(details.ShippingPhone) ||
            string.IsNullOrWhiteSpace(details.ShippingAddress) ||
            string.IsNullOrWhiteSpace(details.ShippingCity) ||
            string.IsNullOrWhiteSpace(details.ShippingState))
        {
            return ServiceResult<Order>.Fail("Please complete your delivery details.");
        }

        var cart = await _db.CartItems
            .Include(x => x.Product)
            .ThenInclude(x => x!.Owner)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        if (cart.Count == 0)
        {
            return ServiceResult<Order>.Fail("Your cart is empty.");
        }

        foreach (var line in cart)
        {
            var product = line.Product;
            if (product is null || !product.IsActive || product.Stage != ProductStage.Retail)
            {
                return ServiceResult<Order>.Fail(
                    $"{product?.Name ?? "An item"} is no longer available. Please review your cart.");
            }

            if (line.Quantity > product.Quantity)
            {
                return ServiceResult<Order>.Fail(
                    $"Only {product.Quantity} {product.Unit} of {product.Name} remain.");
            }
        }

        var subtotal = cart.Sum(x => x.Quantity * x.Product!.Price);
        var delivery = PricingRules.DeliveryFor(subtotal);
        var total = subtotal + delivery;
        var reference = await GenerateReferenceAsync(ct);
        var isCash = details.PaymentMethod.Contains("cash", StringComparison.OrdinalIgnoreCase);

        var order = new Order
        {
            CustomerId = customer.Id,
            Reference = reference,
            Status = isCash ? OrderStatus.Pending : OrderStatus.Paid,
            Subtotal = subtotal,
            DeliveryFee = delivery,
            Total = total,
            PaymentMethod = string.IsNullOrWhiteSpace(details.PaymentMethod)
                ? "Cash on delivery"
                : details.PaymentMethod.Trim(),
            ShippingName = details.ShippingName.Trim(),
            ShippingPhone = details.ShippingPhone.Trim(),
            ShippingAddress = details.ShippingAddress.Trim(),
            ShippingCity = details.ShippingCity.Trim(),
            ShippingState = details.ShippingState.Trim(),
            Notes = (details.Notes ?? string.Empty).Trim(),
            CreatedAt = DateTime.UtcNow,
            PaidAt = isCash ? null : DateTime.UtcNow
        };

        foreach (var line in cart)
        {
            var product = line.Product!;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                SellerId = product.OwnerId,
                ProductName = product.Name,
                SellerName = product.Owner?.StoreName ?? product.Owner?.DisplayName ?? "Seller",
                Unit = product.Unit,
                ImageUrl = product.ImageUrl,
                UnitPrice = product.Price,
                Quantity = line.Quantity
            });

            product.Quantity -= line.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            if (product.Quantity <= 0)
            {
                product.Quantity = 0;
                product.IsActive = false;
                product.Stage = ProductStage.Sold;
            }

            _db.Transfers.Add(new SupplyChainTransfer
            {
                ProductId = product.Id,
                FromUserId = product.OwnerId,
                ToUserId = customer.Id,
                FromStage = ProductStage.Retail,
                ToStage = ProductStage.Sold,
                UnitPrice = product.Price,
                Quantity = line.Quantity,
                Note = $"Order {reference}",
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<Order>.Ok(order);
    }

    public async Task<IReadOnlyList<Order>> GetForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetForSellerAsync(Guid sellerId, CancellationToken ct = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Customer)
            .Where(x => x.Items.Any(i => i.SellerId == sellerId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Customer)
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync(ct);
    }

    public Task<Order?> GetAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public async Task<ServiceResult> UpdateStatusAsync(
        Guid orderId,
        Guid actorId,
        UserRole actorRole,
        OrderStatus status,
        CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (order is null)
        {
            return ServiceResult.Fail("Order not found.");
        }

        var isSeller = order.Items.Any(i => i.SellerId == actorId);
        var isCustomer = order.CustomerId == actorId;
        var isAdmin = actorRole == UserRole.Admin;

        if (!isAdmin && !isSeller && !isCustomer)
        {
            return ServiceResult.Fail("You don't have access to this order.");
        }

        if (isCustomer && !isAdmin && status != OrderStatus.Cancelled)
        {
            return ServiceResult.Fail("Customers can only cancel an order.");
        }

        if (!IsAllowed(order.Status, status))
        {
            return ServiceResult.Fail(
                $"Can't move an order from {order.Status.ToDisplay()} to {status.ToDisplay()}.");
        }

        if (status == OrderStatus.Cancelled)
        {
            if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            {
                return ServiceResult.Fail("This order can no longer be cancelled.");
            }

            foreach (var item in order.Items)
            {
                var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId, ct);
                if (product is null)
                {
                    continue;
                }

                product.Quantity += item.Quantity;
                product.IsActive = true;
                if (product.Stage == ProductStage.Sold)
                {
                    product.Stage = ProductStage.Retail;
                }

                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        order.Status = status;
        if (status == OrderStatus.Paid)
        {
            order.PaidAt = DateTime.UtcNow;
        }
        else if (status == OrderStatus.Delivered)
        {
            order.DeliveredAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private async Task<string> GenerateReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var reference = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
            if (!await _db.Orders.AnyAsync(x => x.Reference == reference, ct))
            {
                return reference;
            }
        }

        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private static bool IsAllowed(OrderStatus from, OrderStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return from switch
        {
            OrderStatus.Pending => to is OrderStatus.Paid or OrderStatus.Cancelled,
            OrderStatus.Paid => to is OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.Processing => to is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => to is OrderStatus.Delivered,
            _ => false
        };
    }
}
