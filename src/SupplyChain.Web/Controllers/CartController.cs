using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[Authorize]
public class CartController : AppController
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;

    public CartController(ICartService cart, IOrderService orders, CurrentUser current) : base(current)
    {
        _cart = cart;
        _orders = orders;
    }

    [HttpGet("/cart")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var items = await _cart.GetAsync(user.Id, ct);
        var subtotal = items.Sum(x => x.UnitPrice * x.Quantity);

        return View(new CartViewModel
        {
            Items = items,
            Subtotal = subtotal,
            DeliveryFee = PricingRules.DeliveryFor(subtotal),
            Total = PricingRules.GrandTotal(subtotal)
        });
    }

    [HttpPost("/cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, int quantity = 1, string? returnUrl = null, CancellationToken ct = default)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Unauthorized(new { success = false, message = "Please sign in first." });
        }

        var result = await _cart.AddAsync(user.Id, productId, quantity, ct);
        var count = await _cart.GetCountAsync(user.Id, ct);

        if (IsAjax)
        {
            return Json(new
            {
                success = result.Succeeded,
                message = result.Succeeded ? "Added to cart" : result.Error,
                count
            });
        }

        if (result.Succeeded)
        {
            Success("Added to cart.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, int quantity, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _cart.UpdateQuantityAsync(user.Id, id, quantity, ct);
        if (result.Succeeded)
        {
            Success("Cart updated.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        await _cart.RemoveAsync(user.Id, id, ct);
        Success("Item removed from your cart.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        await _cart.ClearAsync(user.Id, ct);
        Note("Your cart is empty again.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/checkout")]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildCheckoutAsync(user, null, ct);
        if (model.Items.Count == 0)
        {
            Note("Add something to your cart before checking out.");
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model = await BuildCheckoutAsync(user, model, ct);
            return View(model);
        }

        var result = await _orders.CheckoutAsync(user.Id, new CheckoutDetails
        {
            ShippingName = model.ShippingName,
            ShippingPhone = model.ShippingPhone,
            ShippingAddress = model.ShippingAddress,
            ShippingCity = model.ShippingCity,
            ShippingState = model.ShippingState,
            PaymentMethod = model.PaymentMethod,
            Notes = model.Notes ?? string.Empty
        }, ct);

        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model = await BuildCheckoutAsync(user, model, ct);
            return View(model);
        }

        Success($"Order {result.Value.Reference} placed. We're on it!");
        return RedirectToAction("Details", "Orders", new { id = result.Value.Id });
    }

    private bool IsAjax => string.Equals(
        Request.Headers["X-Requested-With"],
        "XMLHttpRequest",
        StringComparison.OrdinalIgnoreCase);

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Marketplace");
    }

    private async Task<CheckoutViewModel> BuildCheckoutAsync(
        AppUser user,
        CheckoutViewModel? posted,
        CancellationToken ct)
    {
        var items = await _cart.GetAsync(user.Id, ct);
        var subtotal = items.Sum(x => x.UnitPrice * x.Quantity);
        var delivery = PricingRules.DeliveryFor(subtotal);

        return new CheckoutViewModel
        {
            Items = items,
            Subtotal = subtotal,
            DeliveryFee = delivery,
            Total = subtotal + delivery,
            ShippingName = string.IsNullOrWhiteSpace(posted?.ShippingName) ? user.DisplayName : posted!.ShippingName,
            ShippingPhone = string.IsNullOrWhiteSpace(posted?.ShippingPhone) ? user.PhoneNumber : posted!.ShippingPhone,
            ShippingAddress = string.IsNullOrWhiteSpace(posted?.ShippingAddress) ? user.Address : posted!.ShippingAddress,
            ShippingCity = string.IsNullOrWhiteSpace(posted?.ShippingCity) ? user.City : posted!.ShippingCity,
            ShippingState = string.IsNullOrWhiteSpace(posted?.ShippingState) ? user.State : posted!.ShippingState,
            PaymentMethod = string.IsNullOrWhiteSpace(posted?.PaymentMethod) ? "Cash on delivery" : posted!.PaymentMethod,
            Notes = posted?.Notes
        };
    }
}
