using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LearnHub.Infrastructure;

/// <summary>
/// Rejects uploads larger than <see cref="MaxBytes"/>. Implements <see cref="IClientModelValidator"/> so the
/// same rule runs in the browser (see <c>wwwroot/js/site.js</c>) before the server re-checks it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxFileSizeAttribute(long maxBytes) : ValidationAttribute, IClientModelValidator
{
    public long MaxBytes { get; } = maxBytes;

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? $"{name} must be {DisplayFormat.FileSize(MaxBytes)} or smaller.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        value is IFormFile file && file.Length > MaxBytes
            ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName), [validationContext.MemberName!])
            : ValidationResult.Success;

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-filesize", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        context.Attributes.TryAdd("data-val-filesize-max", MaxBytes.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>Allows only the listed file extensions (comma separated, e.g. ".jpg,.png").</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AllowedExtensionsAttribute(string extensions) : ValidationAttribute, IClientModelValidator
{
    private readonly string[] _extensions = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? $"{name} must be one of: {string.Join(", ", _extensions)}.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return ValidationResult.Success;
        }

        var extension = Path.GetExtension(file.FileName);
        return _extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName), [validationContext.MemberName!]);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-fileext", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        context.Attributes.TryAdd("data-val-fileext-extensions", string.Join(",", _extensions));
        context.Attributes.TryAdd("accept", string.Join(",", _extensions));
    }
}
