using LearnHub.Infrastructure;
using LearnHub.Services.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LearnHub.Tests.Unit;

public sealed class FileStorageServiceTests : IDisposable
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] PdfHeader = "%PDF-1.7\n%âãÏÓ\n1 0 obj\n"u8.ToArray();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "learnhub-tests", Guid.NewGuid().ToString("N"));
    private readonly FileStorageService _storage;

    public FileStorageServiceTests()
    {
        _storage = new FileStorageService(
            Options.Create(new StorageOptions { RootPath = _root }),
            new FakeEnvironment(),
            NullLogger<FileStorageService>.Instance);
    }

    [Fact]
    public async Task Saves_a_valid_image_with_a_random_name_and_detected_content_type()
    {
        var result = await SaveAsync(PngHeader, "My Diagram (final).PNG", UploadKind.ResourceImage);

        Assert.True(result.Succeeded, result.Error);
        var stored = result.Value!;
        Assert.StartsWith("resources/", stored.RelativePath);
        Assert.DoesNotContain("Diagram", stored.RelativePath);
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal("My-Diagram-final.png", stored.FileName);
        Assert.NotNull(_storage.GetPhysicalPath(stored.RelativePath));
    }

    [Fact]
    public async Task Rejects_a_file_whose_content_does_not_match_its_extension()
    {
        var html = "<html><script>alert(1)</script></html>"u8.ToArray();

        var result = await SaveAsync(html, "cheat-sheet.pdf", UploadKind.ResourceDocument);

        Assert.False(result.Succeeded);
        Assert.Contains("content does not match", result.Error);
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "resources")));
    }

    [Theory]
    [InlineData("diagram.svg")]
    [InlineData("page.html")]
    [InlineData("script.exe")]
    public async Task Rejects_dangerous_or_unsupported_extensions(string fileName)
    {
        var result = await SaveAsync(PngHeader, fileName, UploadKind.ResourceImage);

        Assert.False(result.Succeeded);
        Assert.Contains("not allowed", result.Error);
    }

    [Fact]
    public async Task Rejects_files_over_the_size_limit_even_if_the_declared_length_is_small()
    {
        var oversized = new byte[UploadRules.MaxImageBytes + 10];
        PngHeader.CopyTo(oversized, 0);

        using var stream = new MemoryStream(oversized);
        var result = await _storage.SaveAsync(stream, "big.png", length: 100, UploadKind.ResourceImage, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("too large", result.Error);
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "resources")));
    }

    [Fact]
    public async Task Accepts_pdf_documents_for_document_resources()
    {
        var result = await SaveAsync(PdfHeader, "notes.pdf", UploadKind.ResourceDocument);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("application/pdf", result.Value!.ContentType);
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("resources/../../outside.txt")]
    public void Paths_outside_the_storage_root_are_never_resolved(string relativePath)
    {
        Assert.Null(_storage.GetPhysicalPath(relativePath));
    }

    [Fact]
    public void Only_uploaded_thumbnails_are_deleted_never_bundled_images()
    {
        var bundled = Path.Combine(_root, "thumbnails", "keep.png");
        File.WriteAllBytes(bundled, PngHeader);

        _storage.DeleteThumbnail("/images/courses/keep.png");

        Assert.True(File.Exists(bundled));
        _storage.DeleteThumbnail("/media/thumbnails/keep.png");
        Assert.False(File.Exists(bundled));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<LearnHub.Services.OperationResult<StoredFile>> SaveAsync(byte[] content, string fileName, UploadKind kind)
    {
        using var stream = new MemoryStream(content);
        return await _storage.SaveAsync(stream, fileName, content.Length, kind);
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "LearnHub.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
