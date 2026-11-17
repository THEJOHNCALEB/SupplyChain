using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Contracts;
using SupplyChain.Web.Services;
using SupplyChain.Web.ViewModels;

namespace SupplyChain.Web.Controllers;

[Authorize]
public class ProfileController : AppController
{
    private readonly IUserService _users;
    private readonly IFileStorage _files;

    public ProfileController(IUserService users, IFileStorage files, CurrentUser current) : base(current)
    {
        _users = users;
        _files = files;
    }

    [HttpGet("/profile")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfilePageViewModel
        {
            User = user,
            Profile = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                StoreName = user.StoreName,
                Bio = user.Bio,
                Address = user.Address,
                City = user.City,
                State = user.State
            }
        });
    }

    [HttpPost("/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "Profile")] ProfileViewModel model, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", new ProfilePageViewModel { User = user, Profile = model });
        }

        var result = await _users.UpdateProfileAsync(user.Id, new ProfileUpdate
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber ?? string.Empty,
            Bio = model.Bio ?? string.Empty,
            StoreName = model.StoreName ?? string.Empty,
            Address = model.Address ?? string.Empty,
            City = model.City ?? string.Empty,
            State = model.State ?? string.Empty
        }, ct);

        if (!result.Succeeded)
        {
            Fail(result.Error);
        }
        else
        {
            Success("Profile saved.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var page = new ProfilePageViewModel
            {
                User = user,
                Profile = ToProfile(user),
                Password = model
            };
            return View("Index", page);
        }

        var result = await _users.ChangePasswordAsync(user.Id, model.CurrentPassword, model.NewPassword, ct);
        if (result.Succeeded)
        {
            Success("Password updated.");
        }
        else
        {
            Fail(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avatar(IFormFile? file, CancellationToken ct)
    {
        var user = await CurrentUserAsync(ct);
        if (user is null)
        {
            return Challenge();
        }

        if (file is null || file.Length == 0)
        {
            Fail("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var saved = await _files.SaveImageAsync(stream, file.FileName, "avatars", ct);
        if (!saved.Succeeded || saved.Value is null)
        {
            Fail(saved.Error);
            return RedirectToAction(nameof(Index));
        }

        var result = await _users.UpdateAvatarAsync(user.Id, saved.Value, ct);
        if (!result.Succeeded)
        {
            _files.Delete(saved.Value);
            Fail(result.Error);
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            _files.Delete(user.AvatarUrl);
        }

        Success("Profile photo updated.");
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToProfile(Domain.Entities.AppUser user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        StoreName = user.StoreName,
        Bio = user.Bio,
        Address = user.Address,
        City = user.City,
        State = user.State
    };
}
