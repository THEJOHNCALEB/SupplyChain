using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Common;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Services;

public sealed class CartService : ICartService
{
    private readonly SupplyChainDbContext _db;

    public CartService(SupplyChainDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CartItem>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.CartItems
            .AsNoTracking()
            .Include(x => x.Product)
            .ThenInclude(x => x!.Owner)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<CartItem>> AddAsync(
        Guid userId,
        Guid productId,
        int quantity = 1,
        CancellationToken ct = default)
    {
        if (quantity < 1)
        {
            return ServiceResult<CartItem>.Fail("Choose at least one unit.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult<CartItem>.Fail("Account not found.");
        }

        if (user.Role is not (UserRole.Customer or UserRole.Admin))
        {
            return ServiceResult<CartItem>.Fail("Only customer accounts can shop the marketplace.");
        }

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);
        if (product is null || !product.IsActive)
        {
            return ServiceResult<CartItem>.Fail("That product is no longer available.");
        }

        if (product.Stage != ProductStage.Retail)
        {
            return ServiceResult<CartItem>.Fail("This lot hasn't reached a retailer yet.");
        }

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);

        var desired = quantity + (existing?.Quantity ?? 0);
        if (desired > product.Quantity)
        {
            return ServiceResult<CartItem>.Fail(
                $"Only {product.Quantity} {product.Unit} available from this seller.");
        }

        if (existing is not null)
        {
            existing.Quantity = desired;
            existing.UnitPrice = product.Price;
            await _db.SaveChangesAsync(ct);
            return ServiceResult<CartItem>.Ok(existing);
        }

        var item = new CartItem
        {
            UserId = userId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = product.Price,
            AddedAt = DateTime.UtcNow
        };

        _db.CartItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<CartItem>.Ok(item);
    }

    public async Task<ServiceResult> UpdateQuantityAsync(
        Guid userId,
        Guid cartItemId,
        int quantity,
        CancellationToken ct = default)
    {
        var item = await _db.CartItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId, ct);

        if (item is null || item.Product is null)
        {
            return ServiceResult.Fail("Item not found in your cart.");
        }

        if (quantity < 1)
        {
            return await RemoveAsync(userId, cartItemId, ct);
        }

        if (!item.Product.IsActive || item.Product.Stage != ProductStage.Retail)
        {
            return ServiceResult.Fail("This product is no longer for sale.");
        }

        if (quantity > item.Product.Quantity)
        {
            return ServiceResult.Fail(
                $"Only {item.Product.Quantity} {item.Product.Unit} available.");
        }

        item.Quantity = quantity;
        item.UnitPrice = item.Product.Price;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveAsync(Guid userId, Guid cartItemId, CancellationToken ct = default)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId, ct);

        if (item is null)
        {
            return ServiceResult.Ok();
        }

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ClearAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _db.CartItems.Where(x => x.UserId == userId).ToListAsync(ct);
        if (items.Count > 0)
        {
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync(ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<decimal> GetTotalAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _db.CartItems
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Quantity, x.UnitPrice })
            .ToListAsync(ct);

        return items.Sum(x => x.Quantity * x.UnitPrice);
    }

    public Task<int> GetCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.CartItems.AsNoTracking().Where(x => x.UserId == userId).SumAsync(x => x.Quantity, ct);
}
