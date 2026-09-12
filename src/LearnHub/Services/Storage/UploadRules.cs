namespace LearnHub.Services.Storage;

public enum UploadKind
{
    CourseThumbnail,
    ResourceImage,
    ResourceDocument
}

/// <summary>A file saved in private storage.</summary>
public sealed record StoredFile(string RelativePath, string FileName, string ContentType, long SizeBytes);

/// <summary>
/// Upload limits shared by the validation attributes (client + server) and the storage service,
/// so the browser check and the authoritative server check always agree.
/// </summary>
public static class UploadRules
{
    public const long MaxImageBytes = 2 * 1024 * 1024;
    public const long MaxDocumentBytes = 10 * 1024 * 1024;

    public const string ImageExtensions = ".jpg,.jpeg,.png,.webp";
    public const string DocumentExtensions = ".pdf";
    public const string ResourceFileExtensions = ImageExtensions + "," + DocumentExtensions;

    public static long MaxBytes(UploadKind kind) =>
        kind == UploadKind.ResourceDocument ? MaxDocumentBytes : MaxImageBytes;

    public static IReadOnlySet<string> Extensions(UploadKind kind) =>
        kind == UploadKind.ResourceDocument ? DocumentExtensionSet : ImageExtensionSet;

    public static IReadOnlySet<string> ContentTypes(UploadKind kind) =>
        kind == UploadKind.ResourceDocument ? DocumentContentTypes : ImageContentTypes;

    private static readonly HashSet<string> ImageExtensionSet =
        new(ImageExtensions.Split(','), StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> DocumentExtensionSet =
        new(DocumentExtensions.Split(','), StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private static readonly HashSet<string> DocumentContentTypes = ["application/pdf"];
}

/// <summary>Detects real file types from their first bytes ("magic numbers") instead of trusting the file name.</summary>
public static class FileSignatures
{
    public sealed record DetectedType(string ContentType, string Extension);

    public const int HeaderLength = 12;

    public static DetectedType? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new DetectedType("image/jpeg", ".jpg");
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return new DetectedType("image/png", ".png");
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return new DetectedType("image/webp", ".webp");
        }

        if (header.Length >= 5 && header[..5].SequenceEqual("%PDF-"u8))
        {
            return new DetectedType("application/pdf", ".pdf");
        }

        return null;
    }
}
