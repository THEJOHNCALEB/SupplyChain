using System.Security.Claims;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Entities;

namespace SupplyChain.Web.Services;

/// <summary>
/// Caches the signed-in user for the duration of a request so layouts and
/// controllers don't each hit the database.
/// </summary>
public sealed class CurrentUser
{
    private readonly IHttpContextAccessor _http;
    private readonly IUserService _users;
    private AppUser? _user;
    private bool _loaded;

    public CurrentUser(IHttpContextAccessor http, IUserService users)
    {
        _http = http;
        _users = users;
    }

    public ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public async Task<AppUser?> GetAsync(CancellationToken ct = default)
    {
        if (_loaded)
        {
            return _user;
        }

        _loaded = true;

        if (Id is null)
        {
            return null;
        }

        _user = await _users.GetByIdAsync(Id.Value, ct);
        return _user;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string DisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.FindFirstValue(ClaimTypes.Email)
        ?? "Guest";

    public static string Avatar(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("Avatar") ?? string.Empty;

    public static string StoreName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("StoreName") ?? string.Empty;

    public static string RoleName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
