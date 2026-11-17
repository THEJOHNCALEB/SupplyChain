using System.ComponentModel.DataAnnotations;
using SupplyChain.Domain.Enums;

namespace SupplyChain.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That email address doesn't look right.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(80, MinimumLength = 2)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression("^[a-zA-Z0-9_.]{3,32}$",
        ErrorMessage = "Use 3-32 letters, numbers, dots or underscores.")]
    public string Username { get; set; } = string.Empty;

    [Required, Phone]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Customer;

    [StringLength(120)]
    [Display(Name = "Farm / business name")]
    public string? StoreName { get; set; }

    [StringLength(400)]
    public string? Bio { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the terms to continue.")]
    [Display(Name = "I agree to the terms")]
    public bool AcceptTerms { get; set; }
}

public class ProfileViewModel
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [StringLength(120)]
    public string? StoreName { get; set; }

    [StringLength(400)]
    public string? Bio { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? State { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
