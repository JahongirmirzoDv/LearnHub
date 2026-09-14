using System.Linq.Expressions;
using LearnHub.Models;
using LearnHub.ViewModels.Shared;

namespace LearnHub.Services;

/// <summary>
/// Reusable LINQ projections. EF Core translates them into a single SQL query, selecting only the
/// columns a view needs instead of loading whole entities.
/// </summary>
public static class CourseProjections
{
    /// <summary>
    /// Course card data. Only published lessons count, and a quiz counts only when it is published and has
    /// questions: the same rules used by progress calculation and the quiz pages.
    /// </summary>
    public static Expression<Func<Course, CourseCardViewModel>> ToCard(string? userId) => course => new CourseCardViewModel
    {
        Id = course.Id,
        Title = course.Title,
        ShortDescription = course.ShortDescription,
        CategoryId = course.CategoryId,
        CategoryName = course.Category.Name,
        CategoryIcon = course.Category.IconName,
        Difficulty = course.Difficulty,
        DurationMinutes = course.DurationMinutes,
        ThumbnailPath = course.ThumbnailPath,
        InstructorName = course.InstructorName,
        ResourceCount = course.Resources.Count(resource => resource.IsPublished),
        QuizCount = course.Quizzes.Count(quiz => quiz.IsPublished && quiz.Questions.Any()),
        EnrollmentCount = course.Enrollments.Count,
        IsEnrolled = userId != null && course.Enrollments.Any(enrollment => enrollment.UserId == userId)
    };
}
