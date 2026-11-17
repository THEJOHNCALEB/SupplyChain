using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Entities;
using SupplyChain.Web.Services;

namespace SupplyChain.Web.Controllers;

public abstract class AppController : Controller
{
    protected AppController(CurrentUser current)
    {
        Current = current;
    }

    protected CurrentUser Current { get; }

    protected void Success(string message) => TempData["Success"] = message;

    protected void Fail(string message) => TempData["Error"] = message;

    protected void Note(string message) => TempData["Info"] = message;

    protected async Task<AppUser?> CurrentUserAsync(CancellationToken ct = default) =>
        await Current.GetAsync(ct);

    protected IActionResult Dashboard() => RedirectToAction("Index", "Dashboard");

    protected IActionResult Home() => RedirectToAction("Index", "Home");
}
