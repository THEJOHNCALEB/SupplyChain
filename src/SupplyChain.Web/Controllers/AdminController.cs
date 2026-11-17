using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Enums;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : AppController
{
    private readonly IUserService _users;
    private readonly IOrderService _orders;

    public AdminController(IUserService users, IOrderService orders, CurrentUser current) : base(current)
    {
        _users = users;
        _orders = orders;
    }

    [HttpGet("/admin")]
    public IActionResult Index() => RedirectToAction(nameof(Users));

    [HttpGet("/admin/users")]
    public async Task<IActionResult> Users(string? search, UserRole? role, CancellationToken ct)
    {
        var all = await _users.GetAllAsync(ct: ct);
        var filtered = await _users.GetAllAsync(search, role, ct);

        return View(new AdminUsersViewModel
        {
            Users = filtered,
            Search = search,
            Role = role,
            Farmers = all.Count(x => x.Role == UserRole.Farmer),
            Distributors = all.Count(x => x.Role == UserRole.Distributor),
            Retailers = all.Count(x => x.Role == UserRole.Retailer),
            Customers = all.Count(x => x.Role == UserRole.Customer),
            Admins = all.Count(x => x.Role == UserRole.Admin)
        });
    }

    [HttpPost("/admin/users/{id:guid}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(Guid id, UserRole role, CancellationToken ct)
    {
        var current = await CurrentUserAsync(ct);
        if (current is null)
        {
            return Challenge();
        }

        if (current.Id == id)
        {
            Fail("You can't change your own role. Ask another admin.");
            return RedirectToAction(nameof(Users));
        }

        var target = await _users.GetByIdAsync(id, ct);
        if (target is null)
        {
            Fail("Account not found.");
            return RedirectToAction(nameof(Users));
        }

        if (target.Role == UserRole.Admin && role != UserRole.Admin)
        {
            var all = await _users.GetAllAsync(role: UserRole.Admin, ct: ct);
            if (all.Count <= 1)
            {
                Fail("At least one admin must remain.");
                return RedirectToAction(nameof(Users));
            }
        }

        var result = await _users.SetRoleAsync(id, role, ct);
        if (result.Succeeded)
        {
            Success($"{target.DisplayName} is now a {role.ToDisplay().ToLowerInvariant()}.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectToAction(nameof(Users), new { search = string.Empty });
    }

    [HttpPost("/admin/users/{id:guid}/active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        var current = await CurrentUserAsync(ct);
        if (current is null)
        {
            return Challenge();
        }

        if (current.Id == id)
        {
            Fail("You can't deactivate your own account.");
            return RedirectToAction(nameof(Users));
        }

        var result = await _users.SetActiveAsync(id, isActive, ct);
        if (result.Succeeded)
        {
            Success(isActive ? "Account re-activated." : "Account deactivated.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpGet("/admin/orders")]
    public async Task<IActionResult> Orders(CancellationToken ct)
    {
        var current = await CurrentUserAsync(ct);
        if (current is null)
        {
            return Challenge();
        }

        var orders = await _orders.GetAllAsync(ct);

        return View(new OrdersViewModel
        {
            User = current,
            Orders = orders,
            IsSellerView = true
        });
    }
}
