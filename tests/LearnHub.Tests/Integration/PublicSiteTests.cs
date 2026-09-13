using System.Net;
using LearnHub.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Integration;

public sealed class PublicSiteTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    [Theory]
    [InlineData("/", "Build real computing skills")]
    [InlineData("/Courses", "Search by topic or instructor")]
    [InlineData("/About", "About LearnHub")]
    [InlineData("/Contact", "Contact the LearnHub team")]
    [InlineData("/Privacy", "Privacy notice")]
    [InlineData("/Account/Login", "Log in to LearnHub")]
    [InlineData("/Account/Register", "Create your student account")]
    public async Task Public_pages_load_for_guests(string url, string expectedText)
    {
        var html = await factory.CreateBrowserClient().GetHtmlAsync(url);
        Assert.Contains(expectedText, html);
    }

    [Fact]
    public async Task Seeded_catalogue_is_shown_with_published_courses_only()
    {
        var html = await factory.CreateBrowserClient().GetHtmlAsync("/Courses");

        Assert.Contains("C# Programming Fundamentals", html);
        Assert.Contains("Relational Database Design with SQL", html);
        Assert.DoesNotContain("Git and GitHub for Team Projects", html);
    }

    [Fact]
    public async Task Search_is_performed_on_the_server()
    {
        var html = await factory.CreateBrowserClient().GetHtmlAsync("/Courses?q=sql");

        Assert.Contains("Relational Database Design with SQL", html);
        Assert.DoesNotContain("Networking Fundamentals", html);
        Assert.Contains("1 course found", html);
    }

    [Fact]
    public async Task Category_filter_limits_results_to_that_category()
    {
        var categoryId = await factory.WithDbAsync(db => db.Categories.Where(c => c.Name == "Cybersecurity").Select(c => c.Id).SingleAsync());

        var html = await factory.CreateBrowserClient().GetHtmlAsync($"/Courses?categoryId={categoryId}");

        Assert.Contains("Web Application Security Basics", html);
        Assert.DoesNotContain("Cloud Computing Foundations", html);
    }

    [Fact]
    public async Task Course_details_show_route_and_enrolment_call_to_action()
    {
        var courseId = await CourseIdAsync("Networking Fundamentals");

        var html = await factory.CreateBrowserClient().GetHtmlAsync($"/Courses/Details/{courseId}");

        Assert.Contains("Course route", html);
        Assert.Contains("How data travels across a network", html);
        Assert.Contains("Create a free account", html);
    }

    [Theory]
    [InlineData("/Courses/Details/999999")]
    [InlineData("/Resources/Details/999999")]
    [InlineData("/this-page-does-not-exist")]
    public async Task Unknown_pages_and_invalid_ids_return_a_friendly_404(string url)
    {
        var response = await factory.CreateBrowserClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("on the map", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Draft_courses_are_not_visible_to_guests()
    {
        var draftId = await CourseIdAsync("Git and GitHub for Team Projects");

        var response = await factory.CreateBrowserClient().GetAsync($"/Courses/Details/{draftId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Preview_lessons_are_open_to_guests_but_other_lessons_require_login()
    {
        var courseId = await CourseIdAsync("Networking Fundamentals");
        var (previewId, lockedId) = await factory.WithDbAsync(async db =>
        {
            var resources = await db.LearningResources.Where(r => r.CourseId == courseId).ToListAsync();
            return (resources.First(r => r.IsPreview).Id, resources.First(r => !r.IsPreview).Id);
        });
        var client = factory.CreateBrowserClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Resources/Details/{previewId}")).StatusCode);

        var locked = await client.GetAsync($"/Resources/Details/{lockedId}");
        Assert.Equal(HttpStatusCode.Redirect, locked.StatusCode);
        Assert.StartsWith("/Account/Login", locked.LocationPath());
    }

    [Fact]
    public async Task Private_files_cannot_be_downloaded_by_guests()
    {
        var pdfId = await factory.WithDbAsync(db =>
            db.LearningResources.Where(r => r.FileContentType == "application/pdf" && !r.IsPreview).Select(r => r.Id).FirstAsync());

        var response = await factory.CreateBrowserClient().GetAsync($"/Resources/Open/{pdfId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.LocationPath());
    }

    [Fact]
    public async Task Html_responses_carry_security_headers()
    {
        var response = await factory.CreateBrowserClient().GetAsync("/");

        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Health_endpoint_reports_the_database_as_healthy()
    {
        var response = await factory.CreateBrowserClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Static_assets_and_seeded_covers_are_served()
    {
        var client = factory.CreateBrowserClient();

        var css = await client.GetAsync("/css/site.css");
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);

        var cover = await client.GetAsync("/images/courses/networking-fundamentals.svg");
        Assert.Equal(HttpStatusCode.OK, cover.StatusCode);
    }

    [Fact]
    public async Task Tests_run_against_an_isolated_in_memory_database()
    {
        var connectionString = await factory.WithDbAsync(db => Task.FromResult(db.Database.GetConnectionString()));
        Assert.Contains("Mode=Memory", connectionString);
    }

    private Task<int> CourseIdAsync(string title) =>
        factory.WithDbAsync(db => db.Courses.Where(c => c.Title == title).Select(c => c.Id).SingleAsync());
}
