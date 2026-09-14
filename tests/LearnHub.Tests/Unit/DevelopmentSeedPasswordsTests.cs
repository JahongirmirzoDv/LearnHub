using LearnHub.Data.Seed;
using LearnHub.Infrastructure;
using LearnHub.Tests.Infrastructure;
using LearnHub.ViewModels.Account;

namespace LearnHub.Tests.Unit;

public sealed class DevelopmentSeedPasswordsTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "learnhub-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Development_generates_strong_demo_passwords_once_and_reuses_them()
    {
        var generator = new DevelopmentSeedPasswords(Environment("Development"));
        var first = new SeedOptions { AdminEmail = "admin@learnhub.local" };
        var second = new SeedOptions { AdminEmail = "admin@learnhub.local" };

        generator.PostConfigure(null, first);
        generator.PostConfigure(null, second);

        Assert.True(File.Exists(generator.FilePath));
        Assert.NotEqual(first.AdminPassword, first.DemoStudentPassword);
        Assert.Equal(first.AdminPassword, second.AdminPassword);
        Assert.Equal(first.DemoStudentPassword, second.DemoStudentPassword);
        Assert.All([first.AdminPassword!, first.DemoStudentPassword!], password =>
        {
            Assert.True(password.Length >= PasswordRules.MinimumLength);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
        });
    }

    [Fact]
    public void Configured_passwords_are_never_replaced()
    {
        var options = new SeedOptions { AdminPassword = "Configured-Admin1", DemoStudentPassword = "Configured-Student1" };

        new DevelopmentSeedPasswords(Environment("Development")).PostConfigure(null, options);

        Assert.Equal("Configured-Admin1", options.AdminPassword);
        Assert.Equal("Configured-Student1", options.DemoStudentPassword);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Other_environments_never_generate_passwords(string environmentName)
    {
        var generator = new DevelopmentSeedPasswords(Environment(environmentName));
        var options = new SeedOptions();

        generator.PostConfigure(null, options);

        Assert.Null(options.AdminPassword);
        Assert.Null(options.DemoStudentPassword);
        Assert.False(File.Exists(generator.FilePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private TestHostEnvironment Environment(string name) => new() { EnvironmentName = name, ContentRootPath = _contentRoot };
}
