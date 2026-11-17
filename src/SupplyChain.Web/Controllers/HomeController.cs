using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Enums;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[AllowAnonymous]
public class HomeController : AppController
{
    private readonly IProductService _products;
    private readonly IUserService _users;
    private readonly IOrderService _orders;

    public HomeController(
        IProductService products,
        IUserService users,
        IOrderService orders,
        CurrentUser current) : base(current)
    {
        _products = products;
        _users = users;
        _orders = orders;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var featured = await _products.GetFeaturedAsync(6, ct);
        var counts = await _products.GetCategoryCountsAsync(ct);
        var allProducts = await _products.SearchAsync(new ProductQuery { PageSize = 1 }, ct);
        var allUsers = await _users.GetAllAsync(ct: ct);
        var allOrders = await _orders.GetAllAsync(ct);

        return View(new HomeViewModel
        {
            Featured = featured,
            CategoryCounts = counts,
            ProductCount = allProducts.TotalCount,
            PartnerCount = allUsers.Count(x => x.Role is UserRole.Farmer or UserRole.Distributor or UserRole.Retailer),
            CustomerCount = allUsers.Count(x => x.Role == UserRole.Customer),
            DeliveredOrders = allOrders.Count(x => x.Status == OrderStatus.Delivered)
        });
    }

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/contact")]
    public IActionResult Contact() => View();

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/error")]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return View("Status", new ErrorViewModel
        {
            StatusCode = 500,
            Title = "Something went wrong",
            Message = "We hit an unexpected snag. Please try again in a moment.",
            RequestId = feature is null ? requestId : requestId
        });
    }

    [HttpGet("/status/{code:int}")]
    public IActionResult Status(int code)
    {
        var model = code switch
        {
            400 => new ErrorViewModel { StatusCode = 400, Title = "That request didn't add up", Message = "Please go back and try again." },
            401 => new ErrorViewModel { StatusCode = 401, Title = "Please sign in", Message = "You need an account to open this page." },
            403 => new ErrorViewModel { StatusCode = 403, Title = "You don't have access", Message = "This area is reserved for a different role in the chain." },
            404 => new ErrorViewModel { StatusCode = 404, Title = "We couldn't find that page", Message = "The link may be old, or the page may have moved." },
            _ => new ErrorViewModel { StatusCode = code, Title = "Something went wrong", Message = "An unexpected error occurred. Please try again." }
        };

        Response.StatusCode = code;
        return View("Status", model);
    }
}
