using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Enums;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[AllowAnonymous]
public class MarketplaceController : AppController
{
    private readonly IProductService _products;

    public MarketplaceController(IProductService products, CurrentUser current) : base(current)
    {
        _products = products;
    }

    [HttpGet("/marketplace")]
    public async Task<IActionResult> Index(
        string? search,
        ProductCategory? category,
        string? sort,
        int page = 1,
        ProductStage? stage = null,
        CancellationToken ct = default)
    {
        var viewer = await CurrentUserAsync(ct);
        var viewerRole = viewer?.Role;

        var activeStage = ResolveStage(viewerRole, stage);

        var query = new ProductQuery
        {
            Search = search,
            Category = category,
            Stage = activeStage,
            Sort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort,
            Page = page,
            PageSize = 12
        };

        var results = await _products.SearchAsync(query, ct);
        var counts = await _products.GetCategoryCountsAsync(ct);

        return View(new MarketplaceViewModel
        {
            Results = results,
            Query = query,
            CategoryCounts = counts,
            ViewerRole = viewerRole,
            Stage = activeStage ?? ProductStage.Retail
        });
    }

    [HttpGet("/marketplace/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var product = await _products.GetAsync(id, ct);
        if (product is null)
        {
            return NotFound();
        }

        var viewer = await CurrentUserAsync(ct);
        var transfers = await _products.GetTransfersAsync(id, ct);

        var related = await _products.SearchAsync(new ProductQuery
        {
            Category = product.Category,
            Stage = product.Stage,
            PageSize = 4
        }, ct);

        var canAcquire = viewer is not null
            && viewer.Id != product.OwnerId
            && product.NextRole == viewer.Role
            && viewer.Role is UserRole.Distributor or UserRole.Retailer
            && product.IsActive
            && product.Quantity > 0;

        var canBuy = viewer is not null
            && viewer.Role == UserRole.Customer
            && product.Stage == ProductStage.Retail
            && product.IsActive
            && product.Quantity > 0;

        return View(new ProductDetailsViewModel
        {
            Product = product,
            Transfers = transfers,
            Related = related.Items.Where(x => x.Id != product.Id).Take(3).ToList(),
            CanAcquire = canAcquire,
            CanBuy = canBuy,
            IsOwner = viewer?.Id == product.OwnerId
        });
    }

    [HttpPost("/marketplace/{id:guid}/acquire")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acquire(Guid id, decimal price, string? note, CancellationToken ct)
    {
        var viewer = await CurrentUserAsync(ct);
        if (viewer is null)
        {
            return Challenge();
        }

        var result = await _products.AcquireAsync(new AcquireRequest
        {
            ProductId = id,
            BuyerId = viewer.Id,
            NewPrice = price,
            Note = note ?? string.Empty
        }, ct);

        if (!result.Succeeded)
        {
            Fail(result.Error);
            return RedirectToAction(nameof(Details), new { id });
        }

        Success($"You now own {result.Value?.Name}. It's in your inventory and ready to sell on.");
        return RedirectToAction("Products", "Dashboard");
    }

    private static ProductStage? ResolveStage(UserRole? role, ProductStage? requested)
    {
        return role switch
        {
            UserRole.Distributor => ProductStage.Farm,
            UserRole.Retailer => ProductStage.Distribution,
            UserRole.Admin => requested,
            _ => ProductStage.Retail
        };
    }
}
