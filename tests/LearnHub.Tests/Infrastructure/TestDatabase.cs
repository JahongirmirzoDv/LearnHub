using LearnHub.Data;
using LearnHub.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Infrastructure;

/// <summary>
/// A migrated, empty in-memory SQLite database for service tests. Using the real migrations (not EnsureCreated)
/// means every test run also proves the migrations build a working schema from nothing.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, SqliteDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public SqliteDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite(connection).Options;
        var context = new SqliteDbContext(options);
        await context.Database.MigrateAsync();
        return new TestDatabase(connection, context);
    }

    /// <summary>A second context on the same database, useful to verify what was really saved.</summary>
    public SqliteDbContext NewContext() =>
        new(new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite(_connection).Options);

    public async Task<ApplicationUser> AddUserAsync(string name = "Test Learner")
    {
        var user = new ApplicationUser
        {
            UserName = $"{Guid.NewGuid():N}@learnhub.test",
            Email = $"{Guid.NewGuid():N}@learnhub.test",
            FullName = name
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    public async Task<Category> AddCategoryAsync(string name = "Programming")
    {
        var category = new Category { Name = name, IconName = CategoryIcons.Default };
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();
        return category;
    }

    /// <summary>Adds a course with the given number of resources and, optionally, a published quiz with two questions.</summary>
    public async Task<Course> AddCourseAsync(
        Category category, string title = "Sample course", bool published = true, int resourceCount = 2, bool withQuiz = true)
    {
        var course = new Course
        {
            Title = title,
            ShortDescription = "A short description for testing purposes.",
            Description = "A longer description used by automated tests to create a realistic course.",
            InstructorName = "Test Instructor",
            DurationMinutes = 60,
            IsPublished = published,
            CategoryId = category.Id
        };

        for (var index = 1; index <= resourceCount; index++)
        {
            course.Resources.Add(new LearningResource
            {
                Title = $"Lesson {index}",
                Type = ResourceType.Article,
                Body = "Lesson body with enough text for validation rules to be satisfied in tests.",
                SortOrder = index,
                IsPreview = index == 1
            });
        }

        if (withQuiz)
        {
            var quiz = new Quiz { Title = "Checkpoint quiz", PassMarkPercent = 50, IsPublished = true };
            for (var q = 1; q <= 2; q++)
            {
                var question = new Question { Text = $"Question {q}?", SortOrder = q };
                question.Options.Add(new AnswerOption { Text = "Right", IsCorrect = true, SortOrder = 0 });
                question.Options.Add(new AnswerOption { Text = "Wrong", IsCorrect = false, SortOrder = 1 });
                quiz.Questions.Add(question);
            }

            course.Quizzes.Add(quiz);
        }

        Context.Courses.Add(course);
        await Context.SaveChangesAsync();
        return course;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
