using System.Text;
using LearnHub.Infrastructure;
using Microsoft.Extensions.Options;

namespace LearnHub.Services.Storage;

public interface IFileStorageService
{
    /// <summary>Physical folder that holds uploaded course thumbnails (served publicly at <see cref="FileStorageService.ThumbnailRequestPath"/>).</summary>
    string ThumbnailsPath { get; }

    Task<OperationResult<StoredFile>> SaveAsync(IFormFile file, UploadKind kind, CancellationToken cancellationToken = default);

    Task<OperationResult<StoredFile>> SaveAsync(Stream content, string originalFileName, long length, UploadKind kind, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute path of a stored file, or null if it does not exist or escapes the storage root.</summary>
    string? GetPhysicalPath(string? relativePath);

    void Delete(string? relativePath);

    string GetThumbnailUrl(StoredFile file);

    /// <summary>Deletes an uploaded thumbnail. Bundled images (e.g. <c>/images/courses/…</c>) are left untouched.</summary>
    void DeleteThumbnail(string? thumbnailPath);
}

/// <summary>
/// Stores uploads outside <c>wwwroot</c> with random names. A file is accepted only when its extension is
/// allow-listed, its size is within limits and its first bytes match a permitted type; the browser-supplied
/// content type is ignored. SVG and HTML are never accepted because they can carry scripts.
/// </summary>
public sealed class FileStorageService : IFileStorageService
{
    public const string ThumbnailRequestPath = "/media/thumbnails";

    private const string ThumbnailFolder = "thumbnails";
    private const string ResourceFolder = "resources";

    private readonly string _rootPath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IOptions<StorageOptions> options, IHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        var configured = options.Value.RootPath;
        var root = Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
        _rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));

        ThumbnailsPath = Path.Combine(_rootPath, ThumbnailFolder);
        Directory.CreateDirectory(ThumbnailsPath);
        Directory.CreateDirectory(Path.Combine(_rootPath, ResourceFolder));
    }

    public string ThumbnailsPath { get; }

    public async Task<OperationResult<StoredFile>> SaveAsync(IFormFile file, UploadKind kind, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        return await SaveAsync(stream, file.FileName, file.Length, kind, cancellationToken);
    }

    public async Task<OperationResult<StoredFile>> SaveAsync(
        Stream content, string originalFileName, long length, UploadKind kind, CancellationToken cancellationToken = default)
    {
        var maxBytes = UploadRules.MaxBytes(kind);
        if (length <= 0)
        {
            return OperationResult<StoredFile>.Failure("The selected file is empty.");
        }

        if (length > maxBytes)
        {
            return OperationResult<StoredFile>.Failure($"The file is too large. The maximum size is {DisplayFormat.FileSize(maxBytes)}.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!UploadRules.Extensions(kind).Contains(extension))
        {
            return OperationResult<StoredFile>.Failure(
                $"This file type is not allowed. Allowed types: {string.Join(", ", UploadRules.Extensions(kind))}.");
        }

        var header = new byte[FileSignatures.HeaderLength];
        var headerLength = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var detected = FileSignatures.Detect(header.AsSpan(0, headerLength));
        if (detected is null || !UploadRules.ContentTypes(kind).Contains(detected.ContentType))
        {
            _logger.LogWarning("Rejected upload {FileName}: content does not match an allowed file type.", originalFileName);
            return OperationResult<StoredFile>.Failure("The file content does not match an allowed file type.");
        }

        var folder = kind == UploadKind.CourseThumbnail ? ThumbnailFolder : ResourceFolder;
        var storedName = $"{Guid.NewGuid():N}{detected.Extension}";
        var physicalPath = Path.Combine(_rootPath, folder, storedName);

        long written;
        await using (var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await output.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);
            await content.CopyToAsync(output, cancellationToken);
            written = output.Length;
        }

        // The declared length comes from the client; enforce the limit on what was actually written.
        if (written > maxBytes)
        {
            File.Delete(physicalPath);
            return OperationResult<StoredFile>.Failure($"The file is too large. The maximum size is {DisplayFormat.FileSize(maxBytes)}.");
        }

        var stored = new StoredFile($"{folder}/{storedName}", SanitizeFileName(originalFileName, detected.Extension), detected.ContentType, written);
        _logger.LogInformation("Stored upload {StoredPath} ({SizeBytes} bytes, {ContentType}).", stored.RelativePath, written, detected.ContentType);
        return OperationResult<StoredFile>.Success(stored);
    }

    public string? GetPhysicalPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var insideRoot = fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return insideRoot && File.Exists(fullPath) ? fullPath : null;
    }

    public void Delete(string? relativePath)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        if (physicalPath is null)
        {
            return;
        }

        try
        {
            File.Delete(physicalPath);
        }
        catch (IOException exception)
        {
            // A leftover file is harmless; failing the user's delete request would not be.
            _logger.LogWarning(exception, "Could not delete stored file {RelativePath}.", relativePath);
        }
    }

    public string GetThumbnailUrl(StoredFile file) =>
        $"{ThumbnailRequestPath}/{Path.GetFileName(file.RelativePath)}";

    public void DeleteThumbnail(string? thumbnailPath)
    {
        if (thumbnailPath is null || !thumbnailPath.StartsWith(ThumbnailRequestPath + "/", StringComparison.Ordinal))
        {
            return;
        }

        Delete($"{ThumbnailFolder}/{Path.GetFileName(thumbnailPath)}");
    }

    private static string SanitizeFileName(string originalFileName, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(originalFileName));
        var builder = new StringBuilder(baseName.Length);
        foreach (var character in baseName)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or ' ' ? character : '-');
        }

        var cleaned = builder.ToString().Trim(' ', '-');
        if (cleaned.Length > 100)
        {
            cleaned = cleaned[..100];
        }

        return (cleaned.Length == 0 ? "file" : cleaned) + extension;
    }
}
