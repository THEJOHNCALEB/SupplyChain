using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Enums;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[Authorize]
public class OrdersController : AppController
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders, CurrentUser current) : base(current)
    {
        _orders = orders;
    }

    [HttpGet("/orders")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (user.Role is UserRole.Farmer or UserRole.Distributor)
        {
            return RedirectToAction("Sales", "Dashboard");
        }

        if (user.Role == UserRole.Retailer)
        {
            return RedirectToAction("Sales", "Dashboard");
        }

        var orders = user.Role == UserRole.Admin
            ? await _orders.GetAllAsync(ct)
            : await _orders.GetForCustomerAsync(user.Id, ct);

        return View(new OrdersViewModel
        {
            User = user,
            Orders = orders,
            IsSellerView = user.Role == UserRole.Admin
        });
    }

    [HttpGet("/orders/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var order = await _orders.GetAsync(id, ct);
        if (order is null)
        {
            return NotFound();
        }

        var isCustomer = order.CustomerId == user.Id;
        var isSeller = order.Items.Any(x => x.SellerId == user.Id);
        var isAdmin = user.Role == UserRole.Admin;

        if (!isCustomer && !isSeller && !isAdmin)
        {
            return Forbid();
        }

        ViewData["IsCustomer"] = isCustomer;
        ViewData["CanManage"] = isSeller || isAdmin;
        ViewData["ViewerId"] = user.Id;

        return View(order);
    }

    [HttpPost("/orders/{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, OrderStatus status, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _orders.UpdateStatusAsync(id, user.Id, user.Role, status, ct);
        if (result.Succeeded)
        {
            Success($"Order marked as {status.ToDisplay().ToLowerInvariant()}.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
