namespace LearnHub.Models;

/// <summary>
/// Single source of truth for column sizes. Used by the EF Core configurations (database schema)
/// and by ViewModel validation attributes, so the form rules can never drift from the schema.
/// </summary>
public static class FieldLengths
{
    public const int PersonName = 100;
    public const int Bio = 500;
    public const int Email = 256;

    public const int CategoryName = 60;
    public const int CategoryDescription = 300;
    public const int IconName = 40;

    public const int CourseTitle = 120;
    public const int CourseShortDescription = 200;
    public const int CourseDescription = 4000;
    public const int CourseLearningOutcomes = 1000;
    public const int WebPath = 260;
    public const int EnumName = 20;

    public const int ResourceTitle = 150;
    public const int ResourceSummary = 300;
    public const int ResourceBody = 20000;
    public const int Url = 500;
    public const int FileName = 255;
    public const int ContentType = 100;

    public const int QuizTitle = 150;
    public const int QuizDescription = 500;
    public const int QuestionText = 500;
    public const int QuestionExplanation = 500;
    public const int AnswerOptionText = 300;

    public const int ContactSubject = 150;
    public const int ContactMessage = 2000;
}
