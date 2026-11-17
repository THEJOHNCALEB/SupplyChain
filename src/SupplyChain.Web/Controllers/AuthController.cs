using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[AllowAnonymous]
public class AuthController : AppController
{
    private readonly IUserService _users;

    public AuthController(IUserService users, CurrentUser current) : base(current)
    {
        _users = users;
    }

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl)
    {
        if (Current.IsAuthenticated)
        {
            return Dashboard();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _users.ValidateCredentialsAsync(model.Email, model.Password, ct);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return Dashboard();
    }

    [HttpGet("/register")]
    public IActionResult Register()
    {
        if (Current.IsAuthenticated)
        {
            return Dashboard();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!RoleCatalog.ChainOrder.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose how you want to use SupplyChain.");
            return View(model);
        }

        var request = new RegisterRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Username = model.Username,
            PhoneNumber = model.PhoneNumber,
            Password = model.Password,
            Role = model.Role,
            StoreName = model.StoreName,
            Bio = model.Bio
        };

        var result = await _users.RegisterAsync(request, ct);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await SignInAsync(result.Value, true);
        Success($"Welcome to SupplyChain, {result.Value.FirstName}! Your {result.Value.Role.ToDisplay().ToLowerInvariant()} account is ready.");
        return Dashboard();
    }

    [HttpPost("/logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Note("You've been signed out. See you soon.");
        return Home();
    }

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(AppUser user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("Username", user.Username),
            new("StoreName", user.StoreName ?? string.Empty),
            new("Avatar", user.AvatarUrl ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = persistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(persistent ? 7 : 1)
            });
    }
}
