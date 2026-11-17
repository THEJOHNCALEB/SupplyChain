using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Common;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly SupplyChainDbContext _db;

    public ProductService(SupplyChainDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var source = _db.Products
            .AsNoTracking()
            .Include(x => x.Owner)
            .AsQueryable();

        if (!query.IncludeInactive)
        {
            source = source.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                x.Origin.ToLower().Contains(term) ||
                (x.Owner != null && x.Owner.StoreName.ToLower().Contains(term)));
        }

        if (query.Category is not null)
        {
            source = source.Where(x => x.Category == query.Category);
        }

        if (query.Stage is not null)
        {
            source = source.Where(x => x.Stage == query.Stage);
        }

        if (query.OwnerId is not null)
        {
            source = source.Where(x => x.OwnerId == query.OwnerId);
        }

        if (query.FeaturedOnly)
        {
            source = source.Where(x => x.IsFeatured);
        }

        source = query.Sort switch
        {
            "price-asc" => source.OrderBy(x => x.Price),
            "price-desc" => source.OrderByDescending(x => x.Price),
            "name" => source.OrderBy(x => x.Name),
            _ => source.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreatedAt)
        };

        var total = await source.CountAsync(ct);
        var items = await source
            .Skip((Math.Max(query.Page, 1) - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>
        {
            Items = items,
            Page = Math.Max(query.Page, 1),
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int take = 6, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Include(x => x.Owner)
            .Where(x => x.IsActive && x.Quantity > 0)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 24))
            .ToListAsync(ct);
    }

    public Task<Product?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Products
            .AsNoTracking()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Include(x => x.Owner)
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<Product>> CreateAsync(Guid ownerId, ProductCreate create, CancellationToken ct = default)
    {
        var owner = await _db.Users.FirstOrDefaultAsync(x => x.Id == ownerId, ct);
        if (owner is null)
        {
            return ServiceResult<Product>.Fail("Account not found.");
        }

        if (owner.Role != UserRole.Farmer && owner.Role != UserRole.Admin)
        {
            return ServiceResult<Product>.Fail("Only farmers can list a new harvest.");
        }

        var validation = Validate(create);
        if (!validation.Succeeded)
        {
            return ServiceResult<Product>.Fail(validation.Error);
        }

        var product = new Product
        {
            OwnerId = ownerId,
            Name = create.Name.Trim(),
            Reference = $"SC-{Guid.NewGuid():N}".ToUpperInvariant()[..11],
            Category = create.Category,
            Description = create.Description.Trim(),
            Unit = string.IsNullOrWhiteSpace(create.Unit) ? "kg" : create.Unit.Trim(),
            Price = create.Price,
            CostPrice = 0,
            Quantity = create.Quantity,
            InitialQuantity = create.Quantity,
            Origin = (create.Origin ?? string.Empty).Trim(),
            ImageUrl = create.ImageUrl ?? string.Empty,
            Stage = ProductStage.Farm,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        _db.Transfers.Add(new SupplyChainTransfer
        {
            ProductId = product.Id,
            FromUserId = null,
            ToUserId = ownerId,
            FromStage = ProductStage.Farm,
            ToStage = ProductStage.Farm,
            UnitPrice = product.Price,
            Quantity = product.Quantity,
            Note = "Harvest listed",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return ServiceResult<Product>.Ok(product);
    }

    public async Task<ServiceResult<Product>> UpdateAsync(
        Guid ownerId,
        Guid productId,
        ProductCreate update,
        CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);
        if (product is null)
        {
            return ServiceResult<Product>.Fail("That listing no longer exists.");
        }

        if (product.OwnerId != ownerId)
        {
            return ServiceResult<Product>.Fail("You can only edit your own listings.");
        }

        var validation = Validate(update);
        if (!validation.Succeeded)
        {
            return ServiceResult<Product>.Fail(validation.Error);
        }

        product.Name = update.Name.Trim();
        product.Category = update.Category;
        product.Description = update.Description.Trim();
        product.Unit = string.IsNullOrWhiteSpace(update.Unit) ? product.Unit : update.Unit.Trim();
        product.Price = update.Price;
        product.Quantity = update.Quantity;
        product.Origin = (update.Origin ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(update.ImageUrl))
        {
            product.ImageUrl = update.ImageUrl;
        }

        product.IsActive = product.Quantity > 0;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ServiceResult<Product>.Ok(product);
    }

    public async Task<ServiceResult> DeleteAsync(Guid ownerId, Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);
        if (product is null)
        {
            return ServiceResult.Ok();
        }

        if (product.OwnerId != ownerId)
        {
            return ServiceResult.Fail("You can only remove your own listings.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<Product>> AcquireAsync(AcquireRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, ct);
        if (product is null || !product.IsActive)
        {
            return ServiceResult<Product>.Fail("That lot is no longer available.");
        }

        var buyer = await _db.Users.FirstOrDefaultAsync(x => x.Id == request.BuyerId, ct);
        if (buyer is null)
        {
            return ServiceResult<Product>.Fail("Account not found.");
        }

        if (product.OwnerId == buyer.Id)
        {
            return ServiceResult<Product>.Fail("You already own this lot.");
        }

        if (buyer.Role is not (UserRole.Distributor or UserRole.Retailer))
        {
            return ServiceResult<Product>.Fail("Only distributors and retailers can acquire a lot.");
        }

        if (product.NextRole != buyer.Role)
        {
            return ServiceResult<Product>.Fail(
                $"This lot is waiting for a {product.NextRole?.ToDisplay() ?? "customer"}.");
        }

        if (product.Quantity <= 0)
        {
            return ServiceResult<Product>.Fail("This lot is sold out.");
        }

        if (request.NewPrice <= 0)
        {
            return ServiceResult<Product>.Fail("Set a selling price greater than zero.");
        }

        if (request.NewPrice < product.Price)
        {
            return ServiceResult<Product>.Fail("Your selling price can't be below the price you paid.");
        }

        var fromStage = product.Stage;

        _db.Transfers.Add(new SupplyChainTransfer
        {
            ProductId = product.Id,
            FromUserId = product.OwnerId,
            ToUserId = buyer.Id,
            FromStage = fromStage,
            ToStage = buyer.Role == UserRole.Distributor ? ProductStage.Distribution : ProductStage.Retail,
            UnitPrice = request.NewPrice,
            Quantity = product.Quantity,
            Note = string.IsNullOrWhiteSpace(request.Note) ? "Lot acquired" : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        product.CostPrice = product.Price;
        product.Price = request.NewPrice;
        product.OwnerId = buyer.Id;
        product.Stage = buyer.Role == UserRole.Distributor
            ? ProductStage.Distribution
            : ProductStage.Retail;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ServiceResult<Product>.Ok(product);
    }

    public async Task<IReadOnlyList<SupplyChainTransfer>> GetTransfersAsync(Guid productId, CancellationToken ct = default)
    {
        return await _db.Transfers
            .AsNoTracking()
            .Include(x => x.FromUser)
            .Include(x => x.ToUser)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SupplyChainTransfer>> GetTransfersBySellerAsync(Guid sellerId, CancellationToken ct = default)
    {
        return await _db.Transfers
            .AsNoTracking()
            .Include(x => x.FromUser)
            .Include(x => x.ToUser)
            .Include(x => x.Product)
            .Where(x => x.FromUserId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<bool> CanAcquireAsync(Product product, UserRole buyerRole)
    {
        var allowed = buyerRole is UserRole.Distributor or UserRole.Retailer
            && product.IsActive
            && product.Quantity > 0
            && product.NextRole == buyerRole;

        return Task.FromResult(allowed);
    }

    public async Task<IReadOnlyDictionary<ProductCategory, int>> GetCategoryCountsAsync(CancellationToken ct = default)
    {
        var rows = await _db.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Category, x => x.Count);
    }

    private static ServiceResult Validate(ProductCreate create)
    {
        if (string.IsNullOrWhiteSpace(create.Name) || create.Name.Trim().Length < 3)
        {
            return ServiceResult.Fail("Give this lot a name of at least 3 characters.");
        }

        if (create.Name.Trim().Length > 160)
        {
            return ServiceResult.Fail("Names must be 160 characters or fewer.");
        }

        if (create.Price <= 0)
        {
            return ServiceResult.Fail("Price must be greater than zero.");
        }

        if (create.Quantity <= 0)
        {
            return ServiceResult.Fail("Quantity must be greater than zero.");
        }

        if (create.Quantity > 1_000_000)
        {
            return ServiceResult.Fail("That quantity is too large. Split it into multiple lots.");
        }

        return ServiceResult.Ok();
    }
}
