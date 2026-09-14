using System.Net;
using LearnHub.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Integration;

public sealed class AdminManagementTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 1];
    private static readonly byte[] PdfBytes = "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF\n"u8.ToArray();

    [Fact]
    public async Task Category_create_edit_and_delete()
    {
        var admin = await AdminClientAsync();
        var name = $"Data Science {Guid.NewGuid():N}"[..20];

        var create = await admin.SubmitFormAsync("/Admin/Categories/Create", "/Admin/Categories/Create", new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Description"] = "Statistics, machine learning and data visualisation.",
            ["IconName"] = "graph-up"
        });
        Assert.Equal("/Admin/Categories", create.LocationPath());
        Assert.Contains(name, await admin.GetHtmlAsync("/Admin/Categories"));

        var id = await factory.WithDbAsync(db => db.Categories.Where(c => c.Name == name).Select(c => c.Id).SingleAsync());

        var edit = await admin.SubmitFormAsync($"/Admin/Categories/Edit/{id}", $"/Admin/Categories/Edit/{id}", new Dictionary<string, string>
        {
            ["Name"] = name + " AI",
            ["IconName"] = "robot"
        });
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        Assert.Equal("robot", await factory.WithDbAsync(db => db.Categories.Where(c => c.Id == id).Select(c => c.IconName).SingleAsync()));

        var delete = await admin.SubmitFormAsync($"/Admin/Categories/Delete/{id}", $"/Admin/Categories/Delete/{id}", []);
        Assert.Equal("/Admin/Categories", delete.LocationPath());
        Assert.False(await factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.Id == id)));
    }

    [Fact]
    public async Task Category_with_courses_is_protected_from_deletion()
    {
        var admin = await AdminClientAsync();
        var id = await factory.WithDbAsync(db => db.Categories.Where(c => c.Name == "Programming").Select(c => c.Id).SingleAsync());

        var html = await admin.GetHtmlAsync($"/Admin/Categories/Delete/{id}");
        Assert.Contains("be deleted yet", html);
        Assert.Contains("still contains", html);

        // Even a hand-crafted POST is refused by the service.
        var post = await admin.SubmitFormAsync($"/Admin/Categories/Delete/{id}", $"/Admin/Categories/Delete/{id}", []);
        Assert.Equal($"/Admin/Categories/Delete/{id}", post.LocationPath());
        Assert.True(await factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.Id == id)));
    }

    [Fact]
    public async Task Category_validation_errors_are_shown()
    {
        var admin = await AdminClientAsync();

        var response = await admin.SubmitFormAsync("/Admin/Categories/Create", "/Admin/Categories/Create", new Dictionary<string, string>
        {
            ["Name"] = "",
            ["IconName"] = "not-an-icon"
        });

        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please enter a category name.", html);
    }

    [Fact]
    public async Task Course_create_with_cover_edit_publish_and_delete()
    {
        var admin = await AdminClientAsync();
        var categoryId = await factory.WithDbAsync(db => db.Categories.Where(c => c.Name == "Databases").Select(c => c.Id).SingleAsync());

        var invalid = await admin.SubmitMultipartFormAsync("/Admin/Courses/Create", "/Admin/Courses/Create",
            CourseFields(categoryId, title: ""), "ThumbnailFile", "cover.png", PngBytes, "image/png");
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Please enter a course title.", await invalid.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));

        var title = $"NoSQL Databases {Guid.NewGuid():N}"[..24];
        var create = await admin.SubmitMultipartFormAsync("/Admin/Courses/Create", "/Admin/Courses/Create",
            CourseFields(categoryId, title), "ThumbnailFile", "cover.png", PngBytes, "image/png");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        var detailsUrl = create.LocationPath();
        Assert.StartsWith("/Admin/Courses/Details/", detailsUrl);
        var id = int.Parse(detailsUrl.Split('/').Last());

        var course = await factory.WithDbAsync(db => db.Courses.SingleAsync(c => c.Id == id));
        Assert.StartsWith("/media/thumbnails/", course.ThumbnailPath);
        Assert.False(course.IsPublished);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(course.ThumbnailPath, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        // Drafts are hidden from the public catalogue until published.
        var guest = factory.CreateBrowserClient();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/Courses/Details/{id}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var edit = await admin.SubmitMultipartFormAsync($"/Admin/Courses/Edit/{id}", $"/Admin/Courses/Edit/{id}",
            CourseFields(categoryId, title + " v2"), "ThumbnailFile", "cover.png", PngBytes, "image/png");
        Assert.Equal(detailsUrl, edit.LocationPath());

        var publish = await admin.SubmitFormAsync(detailsUrl, $"/Admin/Courses/TogglePublished/{id}", []);
        Assert.Equal(HttpStatusCode.Redirect, publish.StatusCode);
        Assert.Contains(title + " v2", await guest.GetHtmlAsync($"/Courses/Details/{id}"));

        var delete = await admin.SubmitFormAsync($"/Admin/Courses/Delete/{id}", $"/Admin/Courses/Delete/{id}", []);
        Assert.Equal("/Admin/Courses", delete.LocationPath());
        Assert.False(await factory.WithDbAsync(db => db.Courses.AnyAsync(c => c.Id == id)));
    }

    [Fact]
    public async Task Resources_are_created_validated_streamed_and_deleted()
    {
        var admin = await AdminClientAsync();
        var courseId = await factory.WithDbAsync(db => db.Courses.Where(c => c.Title == "Cloud Computing Foundations").Select(c => c.Id).SingleAsync());

        var article = await admin.SubmitFormAsync($"/Admin/Resources/Create?courseId={courseId}", "/Admin/Resources/Create", new Dictionary<string, string>
        {
            ["CourseId"] = courseId.ToString(),
            ["Title"] = "Choosing an Azure region",
            ["Type"] = "Article",
            ["Body"] = "## Why regions matter\n\nPick a region close to your users to reduce latency and meet data rules.",
            ["SortOrder"] = "10",
            ["EstimatedMinutes"] = "6"
        });
        Assert.Equal($"/Admin/Courses/Details/{courseId}", article.LocationPath());

        var fake = await admin.SubmitMultipartFormAsync($"/Admin/Resources/Create?courseId={courseId}", "/Admin/Resources/Create",
            ResourceFields(courseId, "Fake PDF"), "UploadFile", "notes.pdf", "<html>not a pdf</html>"u8.ToArray(), "application/pdf");
        Assert.Equal(HttpStatusCode.OK, fake.StatusCode);
        Assert.Contains("The file content does not match an allowed file type.", await fake.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));

        var pdf = await admin.SubmitMultipartFormAsync($"/Admin/Resources/Create?courseId={courseId}", "/Admin/Resources/Create",
            ResourceFields(courseId, "Pricing calculator notes"), "UploadFile", "pricing notes.pdf", PdfBytes, "application/pdf");
        Assert.Equal(HttpStatusCode.Redirect, pdf.StatusCode);

        var pdfId = await factory.WithDbAsync(db => db.LearningResources.Where(r => r.Title == "Pricing calculator notes").Select(r => r.Id).SingleAsync());
        var file = await admin.GetAsync($"/Resources/Open/{pdfId}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("application/pdf", file.Content.Headers.ContentType?.MediaType);

        var delete = await admin.SubmitFormAsync($"/Admin/Resources/Delete/{pdfId}", $"/Admin/Resources/Delete/{pdfId}", []);
        Assert.Equal($"/Admin/Courses/Details/{courseId}", delete.LocationPath());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/Resources/Open/{pdfId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Quiz_must_have_questions_before_it_can_be_published()
    {
        var admin = await AdminClientAsync();
        var courseId = await factory.WithDbAsync(db => db.Courses.Where(c => c.Title == "Python for Problem Solving").Select(c => c.Id).SingleAsync());

        var create = await admin.SubmitFormAsync($"/Admin/Quizzes/Create?courseId={courseId}", "/Admin/Quizzes/Create", new Dictionary<string, string>
        {
            ["CourseId"] = courseId.ToString(),
            ["Title"] = "Functions deep dive",
            ["PassMarkPercent"] = "60"
        });
        var detailsUrl = create.LocationPath();
        var quizId = int.Parse(detailsUrl.Split('/').Last());

        await admin.SubmitFormAsync(detailsUrl, $"/Admin/Quizzes/SetPublished/{quizId}", new Dictionary<string, string> { ["isPublished"] = "true" });
        Assert.Contains("Add at least one question before publishing this quiz.", await admin.GetHtmlAsync(detailsUrl));

        var question = await admin.SubmitFormAsync($"/Admin/Questions/Create?quizId={quizId}", "/Admin/Questions/Create", new Dictionary<string, string>
        {
            ["QuizId"] = quizId.ToString(),
            ["Text"] = "What does a function without a return statement return in Python?",
            ["Options[0].Text"] = "None",
            ["Options[1].Text"] = "0",
            ["Options[2].Text"] = "An empty string",
            ["CorrectOptionIndex"] = "0",
            ["SortOrder"] = "1"
        });
        Assert.Equal(detailsUrl, question.LocationPath());

        await admin.SubmitFormAsync(detailsUrl, $"/Admin/Quizzes/SetPublished/{quizId}", new Dictionary<string, string> { ["isPublished"] = "true" });
        var quiz = await factory.WithDbAsync(db => db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Options).SingleAsync(q => q.Id == quizId));
        Assert.True(quiz.IsPublished);
        Assert.Equal(3, quiz.Questions.Single().Options.Count);
        Assert.Single(quiz.Questions.Single().Options, o => o.IsCorrect);
    }

    [Fact]
    public async Task User_roles_deactivation_and_self_protection()
    {
        var admin = await AdminClientAsync();
        var studentClient = factory.CreateBrowserClient();
        var email = await studentClient.RegisterStudentAsync("Managed Student");
        var userId = await factory.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
        var detailsUrl = $"/Admin/Users/Details/{userId}";

        await admin.SubmitFormAsync(detailsUrl, $"/Admin/Users/Deactivate/{userId}", []);
        var blocked = await factory.CreateBrowserClient().LoginAsync(email, LearnHubWebApplicationFactory.NewUserPassword);
        Assert.Contains("This account is locked.", await blocked.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));

        await admin.SubmitFormAsync(detailsUrl, $"/Admin/Users/Reactivate/{userId}", []);
        var allowed = await factory.CreateBrowserClient().LoginAsync(email, LearnHubWebApplicationFactory.NewUserPassword);
        Assert.Equal(HttpStatusCode.Redirect, allowed.StatusCode);

        await admin.SubmitFormAsync(detailsUrl, $"/Admin/Users/SetRole/{userId}", new Dictionary<string, string> { ["role"] = "Admin" });
        Assert.Contains("Role changed to Admin.", await admin.GetHtmlAsync(detailsUrl));

        var adminId = await factory.WithDbAsync(db => db.Users.Where(u => u.Email == LearnHubWebApplicationFactory.AdminEmail).Select(u => u.Id).SingleAsync());
        var selfDelete = await admin.SubmitFormAsync($"/Admin/Users/Details/{adminId}", $"/Admin/Users/Delete/{adminId}", []);
        Assert.Equal($"/Admin/Users/Details/{adminId}", selfDelete.LocationPath());
        Assert.True(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Id == adminId)));
    }

    [Fact]
    public async Task Enrolments_can_be_managed_by_administrators()
    {
        var admin = await AdminClientAsync();
        var email = await factory.CreateBrowserClient().RegisterStudentAsync("Enrolled By Admin");
        var courseId = await factory.WithDbAsync(db => db.Courses.Where(c => c.Title == "JavaScript Essentials").Select(c => c.Id).SingleAsync());
        var fields = new Dictionary<string, string> { ["StudentEmail"] = email, ["CourseId"] = courseId.ToString() };

        var first = await admin.SubmitFormAsync("/Admin/Enrollments/Create", "/Admin/Enrollments/Create", fields);
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        var duplicate = await admin.SubmitFormAsync("/Admin/Enrollments/Create", "/Admin/Enrollments/Create", fields);
        Assert.Contains("This student is already enrolled in that course.", await duplicate.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));

        var enrollmentId = await factory.WithDbAsync(db => db.Enrollments.Where(e => e.User.Email == email).Select(e => e.Id).SingleAsync());
        var remove = await admin.SubmitFormAsync($"/Admin/Enrollments/Delete/{enrollmentId}", $"/Admin/Enrollments/Delete/{enrollmentId}", []);
        Assert.Equal("/Admin/Enrollments", remove.LocationPath());
        Assert.False(await factory.WithDbAsync(db => db.Enrollments.AnyAsync(e => e.Id == enrollmentId)));
    }

    [Fact]
    public async Task Contact_messages_reach_the_admin_inbox()
    {
        var subject = $"Question {Guid.NewGuid():N}"[..20];
        var send = await factory.CreateBrowserClient().SubmitFormAsync("/Contact", "/Contact", new Dictionary<string, string>
        {
            ["Name"] = "Visitor Person",
            ["Email"] = "visitor@example.com",
            ["Subject"] = subject,
            ["Message"] = "Is there a course about mobile development planned for next semester?"
        });
        Assert.Equal("/Contact", send.LocationPath());

        var admin = await AdminClientAsync();
        Assert.Contains(subject, await admin.GetHtmlAsync("/Admin/Messages?unreadOnly=true"));

        var messageId = await factory.WithDbAsync(db => db.ContactMessages.Where(m => m.Subject == subject).Select(m => m.Id).SingleAsync());
        Assert.Contains("mobile development", await admin.GetHtmlAsync($"/Admin/Messages/Details/{messageId}"));
        Assert.True(await factory.WithDbAsync(db => db.ContactMessages.Where(m => m.Id == messageId).Select(m => m.IsRead).SingleAsync()));

        var delete = await admin.SubmitFormAsync("/Admin/Messages", $"/Admin/Messages/Delete/{messageId}", []);
        Assert.Equal("/Admin/Messages", delete.LocationPath());
    }

    [Fact]
    public async Task Spam_honeypot_submissions_are_not_stored()
    {
        var subject = $"Spam {Guid.NewGuid():N}"[..16];
        await factory.CreateBrowserClient().SubmitFormAsync("/Contact", "/Contact", new Dictionary<string, string>
        {
            ["Name"] = "Bot",
            ["Email"] = "bot@example.com",
            ["Subject"] = subject,
            ["Message"] = "Buy cheap followers now, limited offer for all students!",
            ["Website"] = "https://spam.example"
        });

        Assert.False(await factory.WithDbAsync(db => db.ContactMessages.AnyAsync(m => m.Subject == subject)));
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsAdminAsync();
        return client;
    }

    private static Dictionary<string, string> CourseFields(int categoryId, string title) => new()
    {
        ["Title"] = title,
        ["ShortDescription"] = "Document, key-value and graph databases compared.",
        ["Description"] = "A practical comparison of NoSQL database families and when each one is the right choice for an application.",
        ["InstructorName"] = "Prof. Mei Ling Tan",
        ["CategoryId"] = categoryId.ToString(),
        ["Difficulty"] = "2",
        ["DurationMinutes"] = "240"
    };

    private static Dictionary<string, string> ResourceFields(int courseId, string title) => new()
    {
        ["CourseId"] = courseId.ToString(),
        ["Title"] = title,
        ["Type"] = "Pdf",
        ["SortOrder"] = "11"
    };
}
