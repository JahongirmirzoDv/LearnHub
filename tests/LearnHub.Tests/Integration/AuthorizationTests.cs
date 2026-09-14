using System.Net;
using LearnHub.Tests.Infrastructure;

namespace LearnHub.Tests.Integration;

public sealed class AuthorizationTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    public static TheoryData<string> StudentPages => ["/Student/Dashboard", "/Student/MyCourses", "/Quizzes/History", "/Profile"];

    public static TheoryData<string> AdminPages =>
    [
        "/Admin", "/Admin/Courses", "/Admin/Courses/Create", "/Admin/Categories", "/Admin/Resources", "/Admin/Quizzes",
        "/Admin/QuizAttempts", "/Admin/Users", "/Admin/Enrollments", "/Admin/Messages"
    ];

    [Theory]
    [MemberData(nameof(StudentPages))]
    public async Task Guests_are_sent_to_login_for_member_pages(string url)
    {
        var response = await factory.CreateBrowserClient().GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login?ReturnUrl=", response.LocationPath());
    }

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task Guests_are_sent_to_login_for_admin_pages(string url)
    {
        var response = await factory.CreateBrowserClient().GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.LocationPath());
    }

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task Students_are_denied_access_to_admin_pages(string url)
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsDemoStudentAsync();

        var response = await client.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", response.LocationPath());
    }

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task Administrators_can_open_admin_pages(string url)
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsAdminAsync();

        var response = await client.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(StudentPages))]
    public async Task Students_can_open_their_own_pages(string url)
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsDemoStudentAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Access_denied_page_returns_403()
    {
        var response = await factory.CreateBrowserClient().GetAsync("/Account/AccessDenied", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Students_cannot_post_to_admin_actions_even_with_a_valid_token()
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsDemoStudentAsync();

        // A token from a page the student can open, replayed against an admin action.
        var response = await client.SubmitFormAsync("/Profile", "/Admin/Categories/Create", new Dictionary<string, string>
        {
            ["Name"] = "Hacked category",
            ["IconName"] = "code-slash"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", response.LocationPath());
    }
}
