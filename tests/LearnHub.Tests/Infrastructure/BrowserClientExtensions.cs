using System.Net;
using System.Text.RegularExpressions;

namespace LearnHub.Tests.Infrastructure;

/// <summary>
/// Helpers that use the site the way a browser does: load a page, read its anti-forgery token and post the form.
/// </summary>
public static partial class BrowserClientExtensions
{
    public static async Task<string> GetHtmlAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {url} returned {(int)response.StatusCode}.");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>Loads <paramref name="formPageUrl"/>, then posts <paramref name="fields"/> plus its anti-forgery token to <paramref name="postUrl"/>.</summary>
    public static async Task<HttpResponseMessage> SubmitFormAsync(
        this HttpClient client, string formPageUrl, string postUrl, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var html = await client.GetHtmlAsync(formPageUrl);
        var token = ExtractAntiforgeryToken(html);
        var content = new FormUrlEncodedContent(fields.Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));
        return await client.PostAsync(postUrl, content);
    }

    public static async Task<HttpResponseMessage> SubmitMultipartFormAsync(
        this HttpClient client, string formPageUrl, string postUrl, IEnumerable<KeyValuePair<string, string>> fields,
        string fileField, string fileName, byte[] fileBytes, string contentType)
    {
        var html = await client.GetHtmlAsync(formPageUrl);
        using var content = new MultipartFormDataContent();
        foreach (var (key, value) in fields)
        {
            content.Add(new StringContent(value), key);
        }

        content.Add(new StringContent(ExtractAntiforgeryToken(html)), "__RequestVerificationToken");
        var file = new ByteArrayContent(fileBytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(file, fileField, fileName);
        return await client.PostAsync(postUrl, content);
    }

    public static async Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        await client.SubmitFormAsync("/Account/Login", "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password
        });

    public static async Task LoginAsAdminAsync(this HttpClient client)
    {
        var response = await client.LoginAsync(LearnHubWebApplicationFactory.AdminEmail, LearnHubWebApplicationFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    public static async Task LoginAsDemoStudentAsync(this HttpClient client)
    {
        var response = await client.LoginAsync(LearnHubWebApplicationFactory.DemoStudentEmail, LearnHubWebApplicationFactory.DemoStudentPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>Registers a brand-new student (signed in afterwards) and returns the e-mail address used.</summary>
    public static async Task<string> RegisterStudentAsync(this HttpClient client, string fullName = "Test Student")
    {
        var email = $"student-{Guid.NewGuid():N}@learnhub.test";
        var response = await client.SubmitFormAsync("/Account/Register", "/Account/Register", new Dictionary<string, string>
        {
            ["FullName"] = fullName,
            ["Email"] = email,
            ["Password"] = LearnHubWebApplicationFactory.NewUserPassword,
            ["ConfirmPassword"] = LearnHubWebApplicationFactory.NewUserPassword,
            ["AcceptTerms"] = "true"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return email;
    }

    public static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);
        Assert.True(match.Success, "The page does not contain an anti-forgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static string LocationPath(this HttpResponseMessage response)
    {
        var location = response.Headers.Location ?? throw new InvalidOperationException("The response has no Location header.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*?value=""([^""]+)""")]
    private static partial Regex AntiforgeryTokenPattern();
}
