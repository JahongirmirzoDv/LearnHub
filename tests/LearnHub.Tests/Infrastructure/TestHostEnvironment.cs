using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LearnHub.Tests.Infrastructure;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";

    public string ApplicationName { get; set; } = "LearnHub.Tests";

    public string ContentRootPath { get; set; } = Path.GetTempPath();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>A clock the test moves by hand, so time-dependent values can be asserted exactly.</summary>
internal sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
