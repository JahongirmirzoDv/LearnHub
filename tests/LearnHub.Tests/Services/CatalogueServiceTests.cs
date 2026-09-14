using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Services.Storage;
using LearnHub.Tests.Infrastructure;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LearnHub.Tests.Services;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task Creates_categories_and_rejects_duplicate_names_regardless_of_case()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        var created = await service.CreateAsync(new CategoryFormViewModel { Name = "Databases", IconName = "database" }, cancellationToken: TestContext.Current.CancellationToken);
        var duplicate = await service.CreateAsync(new CategoryFormViewModel { Name = "  DATABASES ", IconName = "database" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(created.Succeeded);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already exists", duplicate.Error);
        Assert.Equal(1, await database.NewContext().Categories.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_category_that_still_has_courses_cannot_be_deleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        await database.AddCourseAsync(category);
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        var result = await service.DeleteAsync(category.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("still contains 1 course", result.Error);
        Assert.True(await database.NewContext().Categories.AnyAsync(c => c.Id == category.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_empty_category_can_be_deleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        Assert.True((await service.DeleteAsync(category.Id, cancellationToken: TestContext.Current.CancellationToken)).Succeeded);
        Assert.True((await service.DeleteAsync(category.Id, cancellationToken: TestContext.Current.CancellationToken)).IsNotFound);
    }
}

public sealed class CourseCatalogServiceTests
{
    [Fact]
    public async Task Search_returns_only_published_courses_matching_the_term_and_filters()
    {
        await using var database = await TestDatabase.CreateAsync();
        var programming = await database.AddCategoryAsync("Programming");
        var data = await database.AddCategoryAsync("Databases");
        await database.AddCourseAsync(programming, "C# Fundamentals");
        await database.AddCourseAsync(data, "SQL Joins in Practice");
        await database.AddCourseAsync(data, "Advanced SQL Tuning", published: false);
        var catalog = CreateCatalog(database);

        var bySql = await catalog.SearchAsync(new CourseSearchQuery { Q = "sql" }, userId: null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(["SQL Joins in Practice"], bySql.Results.Items.Select(c => c.Title));

        var byCategory = await catalog.SearchAsync(new CourseSearchQuery { CategoryId = programming.Id }, userId: null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(["C# Fundamentals"], byCategory.Results.Items.Select(c => c.Title));

        var wildcard = await catalog.SearchAsync(new CourseSearchQuery { Q = "%" }, userId: null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(wildcard.Results.Items);
    }

    [Fact]
    public async Task Search_matches_published_lesson_titles_but_never_drafts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, "Algorithms", resourceCount: 2);
        var lessons = course.Resources.OrderBy(r => r.SortOrder).ToList();
        lessons[0].Title = "Binary search trees";
        lessons[1].Title = "Secret draft topic";
        lessons[1].IsPublished = false;
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = CreateCatalog(database);

        var found = await catalog.SearchAsync(new CourseSearchQuery { Q = "binary search" }, userId: null, TestContext.Current.CancellationToken);
        var draft = await catalog.SearchAsync(new CourseSearchQuery { Q = "secret draft" }, userId: null, TestContext.Current.CancellationToken);

        var card = Assert.Single(found.Results.Items);
        Assert.Equal(["Binary search trees"], card.MatchingLessons);
        Assert.Equal(1, card.ResourceCount);
        Assert.Empty(draft.Results.Items);
    }

    [Fact]
    public async Task Public_categories_count_only_published_courses()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync("Mathematics");
        await database.AddCategoryAsync("Other");
        await database.AddCourseAsync(category, "Discrete maths");
        await database.AddCourseAsync(category, "Hidden draft", published: false);
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        var categories = await service.GetPublicCategoriesAsync(TestContext.Current.CancellationToken);

        var maths = categories.Single(c => c.Name == "Mathematics");
        Assert.Equal(1, maths.CourseCount);
        Assert.Equal(["Discrete maths"], maths.ExampleCourseTitles);
        Assert.Equal(0, categories.Single(c => c.Name == "Other").CourseCount);
    }

    [Fact]
    public async Task Unpublished_course_details_are_hidden_from_everyone_except_admins()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var draft = await database.AddCourseAsync(category, "Draft course", published: false);
        var catalog = CreateCatalog(database);

        Assert.Null(await catalog.GetDetailsAsync(draft.Id, userId: null, isAdmin: false, cancellationToken: TestContext.Current.CancellationToken));
        Assert.NotNull(await catalog.GetDetailsAsync(draft.Id, userId: null, isAdmin: true, cancellationToken: TestContext.Current.CancellationToken));
    }

    private static CourseCatalogService CreateCatalog(TestDatabase database) =>
        new(database.Context,
            new CategoryService(database.Context, NullLogger<CategoryService>.Instance),
            new ProgressService(database.Context, TimeProvider.System));
}

public sealed class EnrollmentAndProgressServiceTests
{
    [Fact]
    public async Task Enrolling_twice_is_rejected_and_unpublished_courses_cannot_be_joined()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category);
        var draft = await database.AddCourseAsync(category, "Draft", published: false);
        var enrollments = CreateEnrollmentService(database);

        Assert.True((await enrollments.EnrollAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken)).Succeeded);
        var second = await enrollments.EnrollAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(second.Succeeded);
        Assert.True((await enrollments.EnrollAsync(user.Id, draft.Id, cancellationToken: TestContext.Current.CancellationToken)).IsNotFound);
        Assert.Equal(1, await database.NewContext().Enrollments.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Progress_counts_completed_lessons_and_passed_quizzes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 3, withQuiz: true);
        var progress = new ProgressService(database.Context, TimeProvider.System);

        Assert.Equal(new(0, 4), await progress.GetForCourseAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken));

        database.Context.ResourceCompletions.Add(new ResourceCompletion { UserId = user.Id, LearningResourceId = course.Resources.First().Id });
        database.Context.QuizAttempts.Add(new QuizAttempt
        {
            UserId = user.Id,
            QuizId = course.Quizzes.First().Id,
            CorrectCount = 2,
            QuestionCount = 2,
            ScorePercent = 100,
            Passed = true
        });
        await database.Context.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await progress.GetForCourseAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, result.CompletedItems);
        Assert.Equal(4, result.TotalItems);
        Assert.Equal(50, result.Percent);
    }

    [Fact]
    public async Task Leaving_a_course_keeps_completed_lessons_for_later()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 2, withQuiz: false);
        var enrollments = CreateEnrollmentService(database);
        await enrollments.EnrollAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken);
        database.Context.ResourceCompletions.Add(new ResourceCompletion { UserId = user.Id, LearningResourceId = course.Resources.First().Id });
        await database.Context.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((await enrollments.LeaveAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken)).Succeeded);
        await enrollments.EnrollAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken);

        var progress = await new ProgressService(database.NewContext(), TimeProvider.System).GetForCourseAsync(user.Id, course.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(50, progress.Percent);
        // Re-enrolling restores the stored progress straight away.
        Assert.Equal(50, (await database.NewContext().Enrollments.SingleAsync(TestContext.Current.CancellationToken)).CompletionPercentage);
    }

    [Fact]
    public async Task Stored_progress_is_recalculated_and_completion_is_cleared_when_new_content_is_added()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 2, withQuiz: false);
        var clock = new ManualClock(new DateTimeOffset(2026, 9, 2, 8, 30, 0, TimeSpan.Zero));
        var progress = new ProgressService(database.Context, clock);
        await CreateEnrollmentService(database).EnrollAsync(user.Id, course.Id, TestContext.Current.CancellationToken);
        foreach (var lesson in course.Resources)
        {
            database.Context.ResourceCompletions.Add(new ResourceCompletion { UserId = user.Id, LearningResourceId = lesson.Id });
        }

        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await progress.SyncAsync(course.Id, cancellationToken: TestContext.Current.CancellationToken);

        var completed = await database.NewContext().Enrollments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(100, completed.CompletionPercentage);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, completed.CompletedAt);

        // A draft lesson changes nothing; publishing it brings the course back below 100%.
        var extra = new LearningResource { CourseId = course.Id, Title = "New lesson", Type = ResourceType.Article, Body = "Body text", SortOrder = 3, IsPublished = false };
        database.Context.LearningResources.Add(extra);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await progress.SyncAsync(course.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(100, (await database.NewContext().Enrollments.SingleAsync(TestContext.Current.CancellationToken)).CompletionPercentage);

        extra.IsPublished = true;
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await progress.SyncAsync(course.Id, cancellationToken: TestContext.Current.CancellationToken);

        var reopened = await database.NewContext().Enrollments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(66, reopened.CompletionPercentage);
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public async Task Draft_lessons_are_hidden_from_students_and_do_not_count_towards_progress()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 2, withQuiz: true);
        var draft = course.Resources.OrderBy(r => r.SortOrder).Last();
        draft.IsPublished = false;
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var enrollments = CreateEnrollmentService(database);
        await enrollments.EnrollAsync(user.Id, course.Id, TestContext.Current.CancellationToken);
        var storageRoot = Path.Combine(Path.GetTempPath(), "learnhub-tests", Guid.NewGuid().ToString("N"));
        var lessons = new LearningResourceService(
            database.Context,
            enrollments,
            new ProgressService(database.Context, TimeProvider.System),
            new FileStorageService(Options.Create(new LearnHub.Infrastructure.StorageOptions { RootPath = storageRoot }), new TestHostEnvironment(), NullLogger<FileStorageService>.Instance),
            TimeProvider.System,
            NullLogger<LearningResourceService>.Instance);

        Assert.Equal(new(0, 2), await new ProgressService(database.Context, TimeProvider.System).GetForCourseAsync(user.Id, course.Id, TestContext.Current.CancellationToken));
        Assert.Equal(LearnHub.ViewModels.Learning.ResourceAccess.NotFound, (await lessons.GetForViewingAsync(draft.Id, user.Id, isAdmin: false, TestContext.Current.CancellationToken)).Access);
        Assert.True((await lessons.ToggleCompletionAsync(draft.Id, user.Id, TestContext.Current.CancellationToken)).IsNotFound);

        var adminView = await lessons.GetForViewingAsync(draft.Id, userId: null, isAdmin: true, TestContext.Current.CancellationToken);
        Assert.Equal(LearnHub.ViewModels.Learning.ResourceAccess.Allowed, adminView.Access);
        Assert.False(adminView.Model!.IsPublished);

        var studentView = await lessons.GetForViewingAsync(course.Resources.OrderBy(r => r.SortOrder).First().Id, user.Id, isAdmin: false, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(studentView.Model!.Outline, item => item.Id == draft.Id);
    }

    private static EnrollmentService CreateEnrollmentService(TestDatabase database) =>
        new(database.Context,
            new ProgressService(database.Context, TimeProvider.System),
            new LookupService(database.Context),
            TimeProvider.System,
            NullLogger<EnrollmentService>.Instance);
}
