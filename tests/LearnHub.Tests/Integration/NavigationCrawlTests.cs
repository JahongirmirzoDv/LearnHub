using System.Net;
using System.Text.RegularExpressions;
using LearnHub.Tests.Infrastructure;

namespace LearnHub.Tests.Integration;

/// <summary>
/// Follows every internal link reachable from the main pages for each role and fails on any broken link
/// (404) or server error (5xx). This covers navigation menus, cards, tables, breadcrumbs and pagination.
/// </summary>
public sealed partial class NavigationCrawlTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    private const int MaxPages = 400;

    [Fact]
    public async Task Guest_navigation_has_no_broken_links()
    {
        await AssertNoBrokenLinksAsync(factory.CreateBrowserClient(), "/");
    }

    [Fact]
    public async Task Student_navigation_has_no_broken_links()
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsDemoStudentAsync();
        await AssertNoBrokenLinksAsync(client, "/Student/Dashboard");
    }

    [Fact]
    public async Task Admin_navigation_has_no_broken_links()
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsAdminAsync();
        await AssertNoBrokenLinksAsync(client, "/Admin");
    }

    private static async Task AssertNoBrokenLinksAsync(HttpClient client, string start)
    {
        var queue = new Queue<string>([start, "/", "/Courses"]);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        while (queue.Count > 0 && visited.Count < MaxPages)
        {
            var url = queue.Dequeue();
            if (!visited.Add(url))
            {
                continue;
            }

            var response = await client.GetAsync(url);
            var status = (int)response.StatusCode;

            if (status is >= 300 and < 400)
            {
                var location = response.LocationPath();
                // Following a redirect to the login page would sign nobody out; everything else is checked too.
                if (location.StartsWith('/'))
                {
                    queue.Enqueue(location);
                }

                continue;
            }

            if (status >= 400)
            {
                failures.Add($"{status} {url}");
                continue;
            }

            if (response.Content.Headers.ContentType?.MediaType != "text/html")
            {
                continue;
            }

            var html = await response.Content.ReadAsStringAsync();
            foreach (Match match in HrefPattern().Matches(html))
            {
                var link = WebUtility.HtmlDecode(match.Groups[1].Value);
                if (link.StartsWith("//", StringComparison.Ordinal) || link.StartsWith("/lib/", StringComparison.Ordinal))
                {
                    continue;
                }

                queue.Enqueue(link.Split('#')[0]);
            }
        }

        Assert.True(failures.Count == 0, "Broken links:\n" + string.Join('\n', failures));
        Assert.True(visited.Count > 10, $"Only {visited.Count} pages were crawled from {start}.");
    }

    [GeneratedRegex("href=\"(/[^\"]*)\"")]
    private static partial Regex HrefPattern();
}
