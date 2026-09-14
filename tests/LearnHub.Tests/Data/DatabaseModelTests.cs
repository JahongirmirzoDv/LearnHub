using LearnHub.Data;
using LearnHub.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Data;

public sealed class DatabaseModelTests
{
    [Fact]
    public void Sqlite_schema_can_be_created_from_the_model()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var db = new ApplicationDbContext(options);

        Assert.True(db.Database.EnsureCreated());
        Assert.Empty(db.Courses);
    }

    [Fact]
    public void Create_script_enforces_the_integrity_rules()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var db = new ApplicationDbContext(options);

        var script = db.Database.GenerateCreateScript();

        Assert.Contains("CREATE UNIQUE INDEX \"IX_Enrollments_UserId_CourseId\"", script);
        Assert.Contains("CREATE UNIQUE INDEX \"IX_Categories_Name\"", script);
        Assert.Contains("CREATE UNIQUE INDEX \"IX_ResourceCompletions_UserId_LearningResourceId\"", script);
        Assert.Contains("CONSTRAINT \"CK_Enrollments_CompletionPercentage\"", script);
        Assert.Contains("CONSTRAINT \"CK_Questions_Points\"", script);
        Assert.Contains("CONSTRAINT \"CK_QuizAttempts_Score\"", script);
    }

    [Theory]
    [InlineData(typeof(Course), nameof(Course.CategoryId), DeleteBehavior.Restrict)]
    [InlineData(typeof(LearningResource), nameof(LearningResource.CourseId), DeleteBehavior.Cascade)]
    [InlineData(typeof(Enrollment), nameof(Enrollment.CourseId), DeleteBehavior.Cascade)]
    [InlineData(typeof(QuizAnswer), nameof(QuizAnswer.QuestionId), DeleteBehavior.Restrict)]
    [InlineData(typeof(QuizAnswer), nameof(QuizAnswer.SelectedOptionId), DeleteBehavior.Restrict)]
    [InlineData(typeof(QuizAttempt), nameof(QuizAttempt.QuizId), DeleteBehavior.Cascade)]
    public void Delete_behaviour_is_intentional(Type entity, string foreignKeyProperty, DeleteBehavior expected)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var db = new ApplicationDbContext(options);

        var foreignKey = db.Model.FindEntityType(entity)!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == foreignKeyProperty));

        Assert.Equal(expected, foreignKey.DeleteBehavior);
    }
}
