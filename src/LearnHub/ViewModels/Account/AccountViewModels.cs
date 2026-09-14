using System.ComponentModel.DataAnnotations;
using LearnHub.Infrastructure;
using LearnHub.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LearnHub.ViewModels.Account;

/// <summary>Password policy shared by Identity options and the form validation (client + server).</summary>
public static class PasswordRules
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 100;

    // At least one lower-case letter, one upper-case letter and one digit.
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";

    public const string Description = "Use at least 8 characters with an upper-case letter, a lower-case letter and a number.";
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(FieldLengths.PersonName, MinimumLength = 2, ErrorMessage = "Full name must be between {2} and {1} characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(FieldLengths.Email)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(PasswordRules.MaximumLength, MinimumLength = PasswordRules.MinimumLength, ErrorMessage = "Password must be between {2} and {1} characters.")]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Description)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "Please accept the terms to create an account.")]
    [Display(Name = "I agree to use LearnHub for learning purposes and accept the privacy notice")]
    public bool AcceptTerms { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(FieldLengths.PersonName, MinimumLength = 2, ErrorMessage = "Full name must be between {2} and {1} characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(FieldLengths.Email, ErrorMessage = "Email address can be at most {1} characters.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Only needed when the email address changes, because the email address is also the sign-in name.</summary>
    [DataType(DataType.Password)]
    [StringLength(PasswordRules.MaximumLength)]
    [Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    [StringLength(FieldLengths.Bio, ErrorMessage = "Bio can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "About me")]
    public string? Bio { get; set; }

    // Display-only values: never bound from the form, so a user cannot post a new role for themselves.
    [BindNever]
    [ValidateNever]
    public DateTime MemberSince { get; set; }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Please enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a new password.")]
    [StringLength(PasswordRules.MaximumLength, MinimumLength = PasswordRules.MinimumLength, ErrorMessage = "Password must be between {2} and {1} characters.")]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Description)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
