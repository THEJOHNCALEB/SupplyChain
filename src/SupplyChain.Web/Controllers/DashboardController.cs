using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Enums;
using SupplyChain.Domain.Entities;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[Authorize]
public class DashboardController : AppController
{
    private readonly IDashboardService _dashboard;
    private readonly IProductService _products;
    private readonly IOrderService _orders;
    private readonly IFileStorage _files;

    public DashboardController(
        IDashboardService dashboard,
        IProductService products,
        IOrderService orders,
        IFileStorage files,
        CurrentUser current) : base(current)
    {
        _dashboard = dashboard;
        _products = products;
        _orders = orders;
        _files = files;
    }

    [HttpGet("/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var snapshot = await _dashboard.GetAsync(user, ct);
        var products = user.Role is UserRole.Farmer or UserRole.Distributor or UserRole.Retailer
            ? await _products.GetByOwnerAsync(user.Id, ct)
            : Array.Empty<Product>();

        return View(new DashboardViewModel
        {
            User = user,
            Snapshot = snapshot,
            Products = products.Take(4).ToList()
        });
    }

    [HttpGet("/dashboard/products")]
    public async Task<IActionResult> Products(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (user.Role == UserRole.Customer)
        {
            Note("Customers shop the marketplace. Sellers manage inventory here.");
            return RedirectToAction("Index", "Marketplace");
        }

        var products = await _products.GetByOwnerAsync(user.Id, ct);
        ViewData["Role"] = user.Role;
        return View(products);
    }

    [HttpGet("/dashboard/products/new")]
    [Authorize(Roles = "Farmer")]
    public async Task<IActionResult> NewProduct(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProductFormViewModel());
    }

    [HttpPost("/dashboard/products/new")]
    [Authorize(Roles = "Farmer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProduct(ProductFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (model.Image is null || model.Image.Length == 0)
        {
            ModelState.AddModelError(nameof(model.Image), "Add a clear photo of the harvest.");
            return View(model);
        }

        await using var stream = model.Image.OpenReadStream();
        var saved = await _files.SaveImageAsync(stream, model.Image.FileName, "products", ct);
        if (!saved.Succeeded || saved.Value is null)
        {
            ModelState.AddModelError(nameof(model.Image), saved.Error);
            return View(model);
        }

        var result = await _products.CreateAsync(user.Id, ToRequest(model, saved.Value), ct);
        if (!result.Succeeded)
        {
            _files.Delete(saved.Value);
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        Success($"{result.Value?.Name} is live in the marketplace.");
        return RedirectToAction(nameof(Products));
    }

    [HttpGet("/dashboard/products/{id:guid}/edit")]
    public async Task<IActionResult> EditProduct(Guid id, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var product = await _products.GetAsync(id, ct);
        if (product is null)
        {
            return NotFound();
        }

        if (product.OwnerId != user.Id)
        {
            return Forbid();
        }

        return View(new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            Unit = product.Unit,
            Price = product.Price,
            Quantity = product.Quantity,
            Origin = product.Origin,
            ExistingImageUrl = product.ImageUrl
        });
    }

    [HttpPost("/dashboard/products/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Guid id, ProductFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var imageUrl = model.ExistingImageUrl ?? string.Empty;

        if (model.Image is { Length: > 0 })
        {
            await using var stream = model.Image.OpenReadStream();
            var saved = await _files.SaveImageAsync(stream, model.Image.FileName, "products", ct);
            if (!saved.Succeeded || saved.Value is null)
            {
                ModelState.AddModelError(nameof(model.Image), saved.Error);
                return View(model);
            }

            imageUrl = saved.Value;
        }

        var result = await _products.UpdateAsync(user.Id, id, ToRequest(model, imageUrl), ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        Success("Listing updated.");
        return RedirectToAction(nameof(Products));
    }

    [HttpPost("/dashboard/products/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _products.DeleteAsync(user.Id, id, ct);
        if (!result.Succeeded)
        {
            Fail(result.Error);
        }
        else
        {
            Success("Listing removed.");
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpGet("/dashboard/sales")]
    public async Task<IActionResult> Sales(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (user.Role == UserRole.Customer)
        {
            return RedirectToAction("Index", "Orders");
        }

        if (user.Role == UserRole.Admin)
        {
            return RedirectToAction("Index", "Admin");
        }

        if (user.Role == UserRole.Retailer)
        {
            var orders = await _orders.GetForSellerAsync(user.Id, ct);
            return View(new SalesViewModel
            {
                User = user,
                Orders = orders,
                ShowOrders = true
            });
        }

        var deals = await _products.GetTransfersBySellerAsync(user.Id, ct);
        return View(new SalesViewModel
        {
            User = user,
            Deals = deals,
            ShowDeals = true
        });
    }

    private static ProductCreate ToRequest(ProductFormViewModel model, string imageUrl) => new()
    {
        Name = model.Name,
        Category = model.Category,
        Description = model.Description ?? string.Empty,
        Unit = model.Unit,
        Price = model.Price,
        Quantity = model.Quantity,
        Origin = model.Origin ?? string.Empty,
        ImageUrl = imageUrl
    };
}
