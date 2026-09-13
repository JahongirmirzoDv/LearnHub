using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Tests.Infrastructure;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearnHub.Tests.Services;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task Creates_categories_and_rejects_duplicate_names_regardless_of_case()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        var created = await service.CreateAsync(new CategoryFormViewModel { Name = "Databases", IconName = "database" });
        var duplicate = await service.CreateAsync(new CategoryFormViewModel { Name = "  DATABASES ", IconName = "database" });

        Assert.True(created.Succeeded);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already exists", duplicate.Error);
        Assert.Equal(1, await database.NewContext().Categories.CountAsync());
    }

    [Fact]
    public async Task A_category_that_still_has_courses_cannot_be_deleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        await database.AddCourseAsync(category);
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        var result = await service.DeleteAsync(category.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("still contains 1 course", result.Error);
        Assert.True(await database.NewContext().Categories.AnyAsync(c => c.Id == category.Id));
    }

    [Fact]
    public async Task An_empty_category_can_be_deleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var service = new CategoryService(database.Context, NullLogger<CategoryService>.Instance);

        Assert.True((await service.DeleteAsync(category.Id)).Succeeded);
        Assert.True((await service.DeleteAsync(category.Id)).IsNotFound);
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

        var bySql = await catalog.SearchAsync(new CourseSearchQuery { Q = "sql" }, userId: null);
        Assert.Equal(["SQL Joins in Practice"], bySql.Results.Items.Select(c => c.Title));

        var byCategory = await catalog.SearchAsync(new CourseSearchQuery { CategoryId = programming.Id }, userId: null);
        Assert.Equal(["C# Fundamentals"], byCategory.Results.Items.Select(c => c.Title));

        var wildcard = await catalog.SearchAsync(new CourseSearchQuery { Q = "%" }, userId: null);
        Assert.Empty(wildcard.Results.Items);
    }

    [Fact]
    public async Task Unpublished_course_details_are_hidden_from_everyone_except_admins()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = await database.AddCategoryAsync();
        var draft = await database.AddCourseAsync(category, "Draft course", published: false);
        var catalog = CreateCatalog(database);

        Assert.Null(await catalog.GetDetailsAsync(draft.Id, userId: null, isAdmin: false));
        Assert.NotNull(await catalog.GetDetailsAsync(draft.Id, userId: null, isAdmin: true));
    }

    private static CourseCatalogService CreateCatalog(TestDatabase database) =>
        new(database.Context,
            new CategoryService(database.Context, NullLogger<CategoryService>.Instance),
            new ProgressService(database.Context));
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

        Assert.True((await enrollments.EnrollAsync(user.Id, course.Id)).Succeeded);
        var second = await enrollments.EnrollAsync(user.Id, course.Id);
        Assert.False(second.Succeeded);
        Assert.True((await enrollments.EnrollAsync(user.Id, draft.Id)).IsNotFound);
        Assert.Equal(1, await database.NewContext().Enrollments.CountAsync());
    }

    [Fact]
    public async Task Progress_counts_completed_lessons_and_passed_quizzes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync();
        var category = await database.AddCategoryAsync();
        var course = await database.AddCourseAsync(category, resourceCount: 3, withQuiz: true);
        var progress = new ProgressService(database.Context);

        Assert.Equal(new(0, 4), await progress.GetForCourseAsync(user.Id, course.Id));

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
        await database.Context.SaveChangesAsync();

        var result = await progress.GetForCourseAsync(user.Id, course.Id);
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
        await enrollments.EnrollAsync(user.Id, course.Id);
        database.Context.ResourceCompletions.Add(new ResourceCompletion { UserId = user.Id, LearningResourceId = course.Resources.First().Id });
        await database.Context.SaveChangesAsync();

        Assert.True((await enrollments.LeaveAsync(user.Id, course.Id)).Succeeded);
        await enrollments.EnrollAsync(user.Id, course.Id);

        var progress = await new ProgressService(database.NewContext()).GetForCourseAsync(user.Id, course.Id);
        Assert.Equal(50, progress.Percent);
    }

    private static EnrollmentService CreateEnrollmentService(TestDatabase database) =>
        new(database.Context,
            new ProgressService(database.Context),
            new LookupService(database.Context),
            TimeProvider.System,
            NullLogger<EnrollmentService>.Instance);
}
