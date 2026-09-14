using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Services.Storage;
using LearnHub.Tests.Infrastructure;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Learning;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LearnHub.Tests.Services;

public sealed class QuizServiceTests
{
    [Fact]
    public async Task Submitting_answers_stores_a_graded_attempt_with_every_answer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, course, quiz) = await CreateEnrolledStudentAsync(database);
        var service = CreateQuizService(database);
        var firstQuestion = quiz.Questions.OrderBy(q => q.SortOrder).First();
        var correctOption = firstQuestion.Options.Single(o => o.IsCorrect);

        var result = await service.SubmitAsync(quiz.Id, user.Id, new Dictionary<int, int> { [firstQuestion.Id] = correctOption.Id }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ResourceAccess.Allowed, result.Access);
        var attempt = await database.NewContext().QuizAttempts.Include(a => a.Answers).SingleAsync(a => a.Id == result.AttemptId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, attempt.CorrectCount);
        Assert.Equal(2, attempt.QuestionCount);
        Assert.Equal(50, attempt.ScorePercent);
        Assert.True(attempt.Passed); // pass mark is 50 in the test data
        Assert.Equal(2, attempt.Answers.Count);
        Assert.Equal(course.Id, (await service.GetResultAsync(attempt.Id, user.Id, isAdmin: false, cancellationToken: TestContext.Current.CancellationToken))!.CourseId);
    }

    [Fact]
    public async Task Students_cannot_open_other_students_results_but_admins_can()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (owner, _, quiz) = await CreateEnrolledStudentAsync(database);
        var other = await database.AddUserAsync("Other Student");
        var service = CreateQuizService(database);
        var submitted = await service.SubmitAsync(quiz.Id, owner.Id, new Dictionary<int, int>(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(await service.GetResultAsync(submitted.AttemptId!.Value, other.Id, isAdmin: false, cancellationToken: TestContext.Current.CancellationToken));
        Assert.NotNull(await service.GetResultAsync(submitted.AttemptId.Value, other.Id, isAdmin: true, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Students_who_are_not_enrolled_cannot_take_or_submit_the_quiz()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category);
        var outsider = await database.AddUserAsync();
        var service = CreateQuizService(database);
        var quizId = course.Quizzes.First().Id;

        Assert.Equal(ResourceAccess.RequiresEnrollment, (await service.GetQuizToTakeAsync(quizId, outsider.Id, cancellationToken: TestContext.Current.CancellationToken)).Access);
        Assert.Equal(ResourceAccess.RequiresEnrollment, (await service.SubmitAsync(quizId, outsider.Id, new Dictionary<int, int>(), cancellationToken: TestContext.Current.CancellationToken)).Access);
        Assert.Equal(0, await database.NewContext().QuizAttempts.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_start_time_comes_from_the_signed_token_issued_with_the_quiz_page()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, _, quiz) = await CreateEnrolledStudentAsync(database);
        var clock = new ManualClock(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
        var service = CreateQuizService(database, clock);

        var take = await service.GetQuizToTakeAsync(quiz.Id, user.Id, cancellationToken: TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(7));
        var submitted = await service.SubmitAsync(quiz.Id, user.Id, new Dictionary<int, int>(), take.Model!.StartToken, TestContext.Current.CancellationToken);

        var attempt = await database.NewContext().QuizAttempts.SingleAsync(a => a.Id == submitted.AttemptId, TestContext.Current.CancellationToken);
        Assert.Equal(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), attempt.StartedAt);
        Assert.Equal(new DateTime(2026, 9, 1, 10, 7, 0, DateTimeKind.Utc), attempt.CompletedAt);
        Assert.Equal(TimeSpan.FromMinutes(7), (await service.GetResultAsync(attempt.Id, user.Id, isAdmin: false, TestContext.Current.CancellationToken))!.TimeTaken);
    }

    [Theory]
    [InlineData("another student")]
    [InlineData("altered token")]
    [InlineData("no token")]
    public async Task An_unusable_start_token_records_an_unknown_start_time(string scenario)
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, course, quiz) = await CreateEnrolledStudentAsync(database);
        var other = await database.AddUserAsync("Other Student");
        database.Context.Enrollments.Add(new Enrollment { UserId = other.Id, CourseId = course.Id });
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var clock = new ManualClock(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
        var service = CreateQuizService(database, clock);
        var token = (await service.GetQuizToTakeAsync(quiz.Id, user.Id, TestContext.Current.CancellationToken)).Model!.StartToken;
        clock.Advance(TimeSpan.FromMinutes(3));

        var (submitter, submittedToken) = scenario switch
        {
            "another student" => (other.Id, token),
            "altered token" => (user.Id, token[..10] + (token[10] == 'A' ? 'B' : 'A') + token[11..]),
            _ => (user.Id, (string?)null)
        };
        var submitted = await service.SubmitAsync(quiz.Id, submitter, new Dictionary<int, int>(), submittedToken, TestContext.Current.CancellationToken);

        var attempt = await database.NewContext().QuizAttempts.SingleAsync(a => a.Id == submitted.AttemptId, TestContext.Current.CancellationToken);
        Assert.Equal(attempt.CompletedAt, attempt.StartedAt);
    }

    [Fact]
    public async Task Passing_the_quiz_updates_the_stored_course_progress()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 0, withQuiz: true);
        var service = CreateQuizService(database);
        await CreateEnrollmentService(database).EnrollAsync(user.Id, course.Id, TestContext.Current.CancellationToken);
        var answers = course.Quizzes.First().Questions.ToDictionary(q => q.Id, q => q.Options.Single(o => o.IsCorrect).Id);

        await service.SubmitAsync(course.Quizzes.First().Id, user.Id, answers, cancellationToken: TestContext.Current.CancellationToken);

        var enrollment = await database.NewContext().Enrollments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(100, enrollment.CompletionPercentage);
        Assert.NotNull(enrollment.CompletedAt);
    }

    [Fact]
    public async Task The_quiz_page_never_contains_the_correct_answers()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, _, quiz) = await CreateEnrolledStudentAsync(database);

        var take = await CreateQuizService(database).GetQuizToTakeAsync(quiz.Id, user.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ResourceAccess.Allowed, take.Access);
        // TakeQuizOption only has Id and Text; this guards against someone adding IsCorrect later.
        Assert.Equal(["Id", "Text"], typeof(TakeQuizOption).GetProperties().Select(p => p.Name).Order());
    }

    internal static async Task<(ApplicationUser User, Course Course, Quiz Quiz)> CreateEnrolledStudentAsync(TestDatabase database)
    {
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category);
        database.Context.Enrollments.Add(new Enrollment { UserId = user.Id, CourseId = course.Id });
        await database.Context.SaveChangesAsync();
        return (user, course, course.Quizzes.First());
    }

    internal static QuizService CreateQuizService(TestDatabase database, TimeProvider? clock = null)
    {
        clock ??= TimeProvider.System;
        return new QuizService(
            database.Context,
            CreateEnrollmentService(database, clock),
            new ProgressService(database.Context, clock),
            new EphemeralDataProtectionProvider(),
            clock,
            NullLogger<QuizService>.Instance);
    }

    internal static EnrollmentService CreateEnrollmentService(TestDatabase database, TimeProvider? clock = null) =>
        new(database.Context,
            new ProgressService(database.Context, clock ?? TimeProvider.System),
            new LookupService(database.Context),
            clock ?? TimeProvider.System,
            NullLogger<EnrollmentService>.Instance);
}

public sealed class QuestionManagementServiceTests
{
    [Fact]
    public async Task Option_ids_that_belong_to_another_question_are_not_modified()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, _, quiz) = await QuizServiceTests.CreateEnrolledStudentAsync(database);
        var questions = quiz.Questions.OrderBy(q => q.SortOrder).ToList();
        var foreignOption = questions[1].Options.First();
        var service = CreateQuestionService(database);

        var model = new QuestionFormViewModel
        {
            Id = questions[0].Id,
            QuizId = quiz.Id,
            Text = "Updated question text?",
            Options =
            [
                new() { Id = foreignOption.Id, Text = "Tampered" },
                new() { Text = "Another answer" }
            ],
            CorrectOptionIndex = 0
        };

        var result = await service.UpdateAsync(questions[0].Id, model, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        await using var verify = database.NewContext();
        Assert.Equal(foreignOption.Text, (await verify.AnswerOptions.SingleAsync(o => o.Id == foreignOption.Id, cancellationToken: TestContext.Current.CancellationToken)).Text);
        Assert.Equal(2, await verify.AnswerOptions.CountAsync(o => o.QuestionId == questions[0].Id, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await verify.AnswerOptions.CountAsync(o => o.QuestionId == questions[0].Id && o.IsCorrect, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Removing_an_answer_that_students_selected_keeps_their_attempts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, _, quiz) = await QuizServiceTests.CreateEnrolledStudentAsync(database);
        var question = quiz.Questions.OrderBy(q => q.SortOrder).First();
        var wrongOption = question.Options.Single(o => !o.IsCorrect);
        var rightOption = question.Options.Single(o => o.IsCorrect);
        await QuizServiceTests.CreateQuizService(database).SubmitAsync(quiz.Id, user.Id, new Dictionary<int, int> { [question.Id] = wrongOption.Id }, cancellationToken: TestContext.Current.CancellationToken);
        var service = CreateQuestionService(database);

        var result = await service.UpdateAsync(question.Id, new QuestionFormViewModel
        {
            Id = question.Id,
            QuizId = quiz.Id,
            Text = question.Text,
            Options = [new() { Id = rightOption.Id, Text = rightOption.Text }, new() { Text = "A brand new wrong answer" }],
            CorrectOptionIndex = 0
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        await using var verify = database.NewContext();
        Assert.False(await verify.AnswerOptions.AnyAsync(o => o.Id == wrongOption.Id, cancellationToken: TestContext.Current.CancellationToken));
        var answer = await verify.QuizAnswers.SingleAsync(a => a.QuestionId == question.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(answer.SelectedOptionId);
        Assert.Equal(1, await verify.QuizAttempts.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Question_points_are_saved_and_used_for_new_attempts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, _, quiz) = await QuizServiceTests.CreateEnrolledStudentAsync(database);
        var questions = quiz.Questions.OrderBy(q => q.SortOrder).ToList();
        var heavy = questions[1];
        var options = heavy.Options.OrderBy(o => o.SortOrder).ToList();

        var update = await CreateQuestionService(database).UpdateAsync(heavy.Id, new QuestionFormViewModel
        {
            Id = heavy.Id,
            QuizId = quiz.Id,
            Text = heavy.Text,
            Points = 3,
            Options = options.Select(o => new AnswerOptionInput { Id = o.Id, Text = o.Text }).ToList(),
            CorrectOptionIndex = options.FindIndex(o => o.IsCorrect)
        }, TestContext.Current.CancellationToken);
        Assert.True(update.Succeeded);

        // Only the three-point question is answered correctly: 3 of 4 points.
        var answers = new Dictionary<int, int> { [heavy.Id] = heavy.Options.Single(o => o.IsCorrect).Id };
        var submitted = await QuizServiceTests.CreateQuizService(database).SubmitAsync(quiz.Id, user.Id, answers, cancellationToken: TestContext.Current.CancellationToken);

        var attempt = await database.NewContext().QuizAttempts.SingleAsync(a => a.Id == submitted.AttemptId, TestContext.Current.CancellationToken);
        Assert.Equal((3, 4, 75), (attempt.Score, attempt.MaxScore, attempt.ScorePercent));
    }

    private static QuestionManagementService CreateQuestionService(TestDatabase database) =>
        new(database.Context, new ProgressService(database.Context, TimeProvider.System), NullLogger<QuestionManagementService>.Instance);
}

public sealed class CourseManagementServiceTests
{
    [Fact]
    public async Task Deleting_a_course_removes_its_content_attempts_and_answers_in_the_right_order()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, course, quiz) = await QuizServiceTests.CreateEnrolledStudentAsync(database);
        await QuizServiceTests.CreateQuizService(database).SubmitAsync(quiz.Id, user.Id, new Dictionary<int, int>(), cancellationToken: TestContext.Current.CancellationToken);
        var storageRoot = Path.Combine(Path.GetTempPath(), "learnhub-tests", Guid.NewGuid().ToString("N"));
        var service = new CourseManagementService(
            database.Context,
            new LookupService(database.Context),
            new FileStorageService(Options.Create(new LearnHub.Infrastructure.StorageOptions { RootPath = storageRoot }), new TestHostEnvironment(), NullLogger<FileStorageService>.Instance),
            NullLogger<CourseManagementService>.Instance);

        var result = await service.DeleteAsync(course.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        await using var verify = database.NewContext();
        Assert.False(await verify.Courses.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.LearningResources.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.Quizzes.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.Questions.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.QuizAttempts.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.QuizAnswers.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await verify.Enrollments.AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(await verify.Users.AnyAsync(u => u.Id == user.Id, cancellationToken: TestContext.Current.CancellationToken));
        Directory.Delete(storageRoot, recursive: true);
    }
}
