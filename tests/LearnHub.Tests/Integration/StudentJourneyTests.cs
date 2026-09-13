using System.Net;
using System.Text.RegularExpressions;
using LearnHub.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
        var beforeEnrol = await client.GetAsync($"/Resources/Details/{course.LockedResourceId}");
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

        // Another student cannot open this result (IDOR protection).
        var otherStudent = factory.CreateBrowserClient();
        await otherStudent.RegisterStudentAsync("Curious Student");
        Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync(resultUrl)).StatusCode);
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
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Resources/Details/{course.LockedResourceId}")).StatusCode);

        var leave = await client.SubmitFormAsync($"/Courses/Details/{course.Id}", $"/Courses/Leave/{course.Id}", []);

        Assert.Equal(HttpStatusCode.Redirect, leave.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/Resources/Details/{course.LockedResourceId}")).StatusCode);
    }

    [Fact]
    public async Task Profile_updates_are_validated_and_saved()
    {
        var client = factory.CreateBrowserClient();
        var email = await client.RegisterStudentAsync("Profile Student");

        var invalid = await client.SubmitFormAsync("/Profile", "/Profile", new Dictionary<string, string> { ["FullName"] = "" });
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Please enter your full name.", await invalid.Content.ReadAsStringAsync());

        var valid = await client.SubmitFormAsync("/Profile", "/Profile", new Dictionary<string, string>
        {
            ["FullName"] = "Renamed Student",
            ["Bio"] = "Second-year software engineering student."
        });
        Assert.Equal(HttpStatusCode.Redirect, valid.StatusCode);

        var saved = await factory.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => new { u.FullName, u.Bio }).SingleAsync());
        Assert.Equal("Renamed Student", saved.FullName);
        Assert.Equal("Second-year software engineering student.", saved.Bio);
    }

    private async Task<CourseFixture> LoadCourseAsync() => await factory.WithDbAsync(async db =>
    {
        var course = await db.Courses.Where(c => c.Title == CourseTitle).Select(c => new { c.Id }).SingleAsync();
        var lockedResourceId = await db.LearningResources
            .Where(r => r.CourseId == course.Id && !r.IsPreview && r.Type == Models.ResourceType.Article)
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
