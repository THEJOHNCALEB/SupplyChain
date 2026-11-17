using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SupplyChain.Domain.Abstractions;
using SupplyChain.Domain.Common;
using SupplyChain.Domain.Contracts;
using SupplyChain.Domain.Entities;
using SupplyChain.Domain.Enums;
using SupplyChain.Infrastructure.Persistence;

namespace SupplyChain.Infrastructure.Services;

public sealed partial class UserService : IUserService
{
    private readonly SupplyChainDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UserService(SupplyChainDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [GeneratedRegex("^[a-z0-9_.]{3,32}$")]
    private static partial Regex UsernameRegex();

    public async Task<ServiceResult<AppUser>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        var email = NormalizeEmail(request.Email);
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        var phone = (request.PhoneNumber ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;

        if (firstName.Length is < 2 or > 80 || lastName.Length is < 2 or > 80)
        {
            return ServiceResult<AppUser>.Fail("Please enter your first and last name.");
        }

        if (!IsValidEmail(email))
        {
            return ServiceResult<AppUser>.Fail("That email address doesn't look right.");
        }

        if (!UsernameRegex().IsMatch(username))
        {
            return ServiceResult<AppUser>.Fail(
                "Usernames must be 3-32 characters using letters, numbers, dots or underscores.");
        }

        if (phone.Length < 7 || phone.Length > 32)
        {
            return ServiceResult<AppUser>.Fail("Please enter a valid phone number.");
        }

        if (password.Length < 8 || password.Length > 128)
        {
            return ServiceResult<AppUser>.Fail("Passwords must be at least 8 characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            return ServiceResult<AppUser>.Fail("Passwords need at least one letter and one number.");
        }

        if (request.Role == UserRole.Admin)
        {
            return ServiceResult<AppUser>.Fail("Admin accounts can only be created by the team.");
        }

        if (await _db.Users.AnyAsync(x => x.Email == email, ct))
        {
            return ServiceResult<AppUser>.Fail("An account with that email already exists.");
        }

        if (await _db.Users.AnyAsync(x => x.Username == username, ct))
        {
            return ServiceResult<AppUser>.Fail("That username is already taken.");
        }

        var user = new AppUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Username = username,
            PhoneNumber = phone,
            PasswordHash = _hasher.Hash(password),
            Role = request.Role,
            StoreName = string.IsNullOrWhiteSpace(request.StoreName)
                ? DefaultStoreName(firstName, request.Role)
                : request.StoreName.Trim(),
            Bio = (request.Bio ?? string.Empty).Trim(),
            IsActive = true,
            IsVerified = request.Role == UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<AppUser>.Fail("We couldn't create that account. Try a different email or username.");
        }

        return ServiceResult<AppUser>.Ok(user);
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalized = NormalizeEmail(email);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalized, ct);

        if (user is null || !user.IsActive || !_hasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = NormalizeEmail(email);
        return _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalized, ct);
    }

    public async Task<ServiceResult<AppUser>> UpdateProfileAsync(Guid userId, ProfileUpdate update, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult<AppUser>.Fail("Account not found.");
        }

        if (string.IsNullOrWhiteSpace(update.FirstName) || string.IsNullOrWhiteSpace(update.LastName))
        {
            return ServiceResult<AppUser>.Fail("Please enter your first and last name.");
        }

        user.FirstName = update.FirstName.Trim();
        user.LastName = update.LastName.Trim();
        user.PhoneNumber = (update.PhoneNumber ?? string.Empty).Trim();
        user.Bio = (update.Bio ?? string.Empty).Trim();
        user.StoreName = (update.StoreName ?? string.Empty).Trim();
        user.Address = (update.Address ?? string.Empty).Trim();
        user.City = (update.City ?? string.Empty).Trim();
        user.State = (update.State ?? string.Empty).Trim();

        await _db.SaveChangesAsync(ct);
        return ServiceResult<AppUser>.Ok(user);
    }

    public async Task<ServiceResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult.Fail("Account not found.");
        }

        if (!_hasher.Verify(currentPassword ?? string.Empty, user.PasswordHash))
        {
            return ServiceResult.Fail("Your current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return ServiceResult.Fail("New passwords must be at least 8 characters.");
        }

        if (!newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
        {
            return ServiceResult.Fail("New passwords need at least one letter and one number.");
        }

        user.PasswordHash = _hasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult.Fail("Account not found.");
        }

        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(
        string? search = null,
        UserRole? role = null,
        CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Email.Contains(term) ||
                x.Username.Contains(term) ||
                x.StoreName.ToLower().Contains(term));
        }

        if (role is not null)
        {
            query = query.Where(x => x.Role == role);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<ServiceResult> SetRoleAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult.Fail("Account not found.");
        }

        user.Role = role;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return ServiceResult.Fail("Account not found.");
        }

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.EndsWith('.') || email.Contains(' '))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string DefaultStoreName(string firstName, UserRole role) => role switch
    {
        UserRole.Farmer => $"{firstName} Farms",
        UserRole.Distributor => $"{firstName} Distribution",
        UserRole.Retailer => $"{firstName} Fresh Market",
        _ => string.Empty
    };
}
