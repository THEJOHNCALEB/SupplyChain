using SupplyChain.Domain.Enums;

namespace SupplyChain.Domain.Entities;

/// <summary>
/// A platform account. Every actor in the chain shares the same account shape
/// and is differentiated by <see cref="Role"/>.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    public string AvatarUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public decimal Balance { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public string Initials
    {
        get
        {
            var first = FirstName.Length > 0 ? FirstName[0].ToString() : string.Empty;
            var last = LastName.Length > 0 ? LastName[0].ToString() : string.Empty;
            return $"{first}{last}".ToUpperInvariant();
        }
    }
}
