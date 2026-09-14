using System.Net;
using System.Text.RegularExpressions;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Tests.Integration;

public sealed partial class StudentJourneyTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    private const string CourseTitle = "Networking Fundamentals";

    [Fact]
    public async Task A_new_student_enrols_studies_takes_the_quiz_and_sees_the_result()
    {
        var client = factory.CreateBrowserClient();
        await client.RegisterStudentAsync("Journey Student");
        var course = await LoadCourseAsync();

        // Before enrolling, a non-preview lesson sends the student back to the course page.
        var beforeEnrol = await client.GetAsync($"/Resources/Details/{course.LockedResourceId}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal($"/Courses/Details/{course.Id}", beforeEnrol.LocationPath());

        // Enrol.
        var enrol = await client.SubmitFormAsync($"/Courses/Details/{course.Id}", $"/Courses/Enroll/{course.Id}", []);
        Assert.Equal($"/Courses/Details/{course.Id}", enrol.LocationPath());
        Assert.Contains("Your progress", await client.GetHtmlAsync($"/Courses/Details/{course.Id}"));
        Assert.Contains(CourseTitle, await client.GetHtmlAsync("/Student/MyCourses"));

        // Study a lesson and mark it complete.
        Assert.Contains("Mark as complete", await client.GetHtmlAsync($"/Resources/Details/{course.LockedResourceId}"));
        var complete = await client.SubmitFormAsync(
            $"/Resources/Details/{course.LockedResourceId}", $"/Resources/ToggleComplete/{course.LockedResourceId}", []);
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Contains("Completed", await client.GetHtmlAsync($"/Resources/Details/{course.LockedResourceId}"));

        // Take the quiz with every answer correct.
        var answers = course.CorrectAnswers.Select(pair => new KeyValuePair<string, string>($"Answers[{pair.Key}]", pair.Value.ToString()));
        var submit = await client.SubmitFormAsync($"/Quizzes/Take/{course.QuizId}", $"/Quizzes/Take/{course.QuizId}", answers);
        Assert.Equal(HttpStatusCode.Redirect, submit.StatusCode);
        var resultUrl = submit.LocationPath();
        Assert.Matches(ResultUrlPattern(), resultUrl);

        var result = await client.GetHtmlAsync(resultUrl);
        Assert.Contains("100%", result);
        Assert.Contains("Passed", result);
        Assert.Contains("Answer review", result);

        Assert.Contains("Networking basics quiz", await client.GetHtmlAsync("/Quizzes/History"));
        Assert.Contains(CourseTitle, await client.GetHtmlAsync("/Student/Dashboard"));

        // The Quizzes and Progress pages reflect the pass, and the enrolment stores the same progress as calculated live.
        var quizzes = await client.GetHtmlAsync("/Quizzes");
        Assert.Contains("Networking basics quiz", quizzes);
        Assert.Contains("Passed", quizzes);
        Assert.Contains(CourseTitle, await client.GetHtmlAsync("/Student/Progress"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LearnHub.Data.ApplicationDbContext>();
            var enrollment = await db.Enrollments.SingleAsync(e => e.CourseId == course.Id && e.User.FullName == "Journey Student", TestContext.Current.CancellationToken);
            var live = await scope.ServiceProvider.GetRequiredService<IProgressService>().GetForCourseAsync(enrollment.UserId, course.Id, TestContext.Current.CancellationToken);
            Assert.Equal(2, live.CompletedItems);
            Assert.Equal(live.Percent, enrollment.CompletionPercentage);
        }

        // Another student cannot open this result (IDOR protection).
        var otherStudent = factory.CreateBrowserClient();
        await otherStudent.RegisterStudentAsync("Curious Student");
        Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync(resultUrl, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Tampered_quiz_answers_are_graded_on_the_server()
    {
        var client = factory.CreateBrowserClient();
        await client.RegisterStudentAsync("Tampering Student");
        var course = await LoadCourseAsync();
        await client.SubmitFormAsync($"/Courses/Details/{course.Id}", $"/Courses/Enroll/{course.Id}", []);

        // Send the correct option of the first question as the answer to every question.
        var firstCorrect = course.CorrectAnswers.First().Value;
        var answers = course.CorrectAnswers.Keys.Select(questionId => new KeyValuePair<string, string>($"Answers[{questionId}]", firstCorrect.ToString()));

        var submit = await client.SubmitFormAsync($"/Quizzes/Take/{course.QuizId}", $"/Quizzes/Take/{course.QuizId}", answers);
        var result = await client.GetHtmlAsync(submit.LocationPath());

        var expectedPercent = 100 / course.CorrectAnswers.Count;
        Assert.Contains($"{expectedPercent}%", result);
        Assert.Contains("Not passed yet", result);
    }

    [Fact]
    public async Task Leaving_a_course_removes_access_to_its_lessons()
    {
        var client = factory.CreateBrowserClient();
        await client.RegisterStudentAsync("Leaving Student");
        var course = await LoadCourseAsync();
        await client.SubmitFormAsync($"/Courses/Details/{course.Id}", $"/Courses/Enroll/{course.Id}", []);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Resources/Details/{course.LockedResourceId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var leave = await client.SubmitFormAsync($"/Courses/Details/{course.Id}", $"/Courses/Leave/{course.Id}", []);

        Assert.Equal(HttpStatusCode.Redirect, leave.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/Resources/Details/{course.LockedResourceId}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Profile_updates_are_validated_and_saved()
    {
        var client = factory.CreateBrowserClient();
        var email = await client.RegisterStudentAsync("Profile Student");

        var invalid = await client.SubmitFormAsync("/Profile", "/Profile", new Dictionary<string, string> { ["FullName"] = "" });
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Please enter your full name.", await invalid.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));

        var valid = await client.SubmitFormAsync("/Profile", "/Profile", new Dictionary<string, string>
        {
            ["FullName"] = "Renamed Student",
            ["Email"] = email,
            ["Bio"] = "Second-year software engineering student."
        });
        Assert.Equal(HttpStatusCode.Redirect, valid.StatusCode);

        var saved = await factory.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => new { u.FullName, u.Bio }).SingleAsync());
        Assert.Equal("Renamed Student", saved.FullName);
        Assert.Equal("Second-year software engineering student.", saved.Bio);
    }

    [Fact]
    public async Task Changing_the_email_address_needs_the_current_password_and_an_unused_address()
    {
        var client = factory.CreateBrowserClient();
        var email = await client.RegisterStudentAsync("Email Student");
        var newEmail = $"renamed-{Guid.NewGuid():N}@learnhub.test";
        Dictionary<string, string> Fields(string address, string? password = null)
        {
            var fields = new Dictionary<string, string> { ["FullName"] = "Email Student", ["Email"] = address };
            if (password is not null)
            {
                fields["CurrentPassword"] = password;
            }

            return fields;
        }

        var withoutPassword = await client.SubmitFormAsync("/Profile", "/Profile", Fields(newEmail));
        Assert.Equal(HttpStatusCode.OK, withoutPassword.StatusCode);
        Assert.Contains("Enter your current password to change your email address.", await withoutPassword.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var taken = await client.SubmitFormAsync("/Profile", "/Profile", Fields(LearnHubWebApplicationFactory.AdminEmail, LearnHubWebApplicationFactory.NewUserPassword));
        Assert.Contains("Another account already uses this email address.", await taken.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // A posted role is ignored: users can never change their own role.
        var fields = Fields(newEmail, LearnHubWebApplicationFactory.NewUserPassword);
        fields["Roles"] = AppRoles.Admin;
        fields["Roles[0]"] = AppRoles.Admin;
        var changed = await client.SubmitFormAsync("/Profile", "/Profile", fields);
        Assert.Equal("/Profile", changed.LocationPath());

        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == newEmail));
        Assert.Equal(newEmail, user.UserName);
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == email)));
        Assert.False(await factory.WithDbAsync(db => db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Admin))));

        // The new address is the sign-in name from now on.
        Assert.Equal(HttpStatusCode.Redirect, (await factory.CreateBrowserClient().LoginAsync(newEmail, LearnHubWebApplicationFactory.NewUserPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateBrowserClient().LoginAsync(email, LearnHubWebApplicationFactory.NewUserPassword)).StatusCode);
    }

    private async Task<CourseFixture> LoadCourseAsync() => await factory.WithDbAsync(async db =>
    {
        var course = await db.Courses.Where(c => c.Title == CourseTitle).Select(c => new { c.Id }).SingleAsync();
        var lockedResourceId = await db.LearningResources
            .Where(r => r.CourseId == course.Id && !r.IsPreview && r.Type == ResourceType.Article)
            .Select(r => r.Id)
            .FirstAsync();
        var quiz = await db.Quizzes
            .Where(q => q.CourseId == course.Id)
            .Select(q => new
            {
                q.Id,
                Answers = q.Questions.Select(question => new { question.Id, Correct = question.Options.Single(o => o.IsCorrect).Id }).ToList()
            })
            .SingleAsync();
        return new CourseFixture(course.Id, lockedResourceId, quiz.Id, quiz.Answers.ToDictionary(a => a.Id, a => a.Correct));
    });

    private sealed record CourseFixture(int Id, int LockedResourceId, int QuizId, Dictionary<int, int> CorrectAnswers);

    [GeneratedRegex(@"^/Quizzes/Result/\d+$")]
    private static partial Regex ResultUrlPattern();
}
