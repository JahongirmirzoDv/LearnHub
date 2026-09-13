using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal sealed record SeedCategory(string Name, string Description, string Icon);

internal sealed record SeedResource(
    string Title,
    ResourceType Type,
    string? Summary,
    int? Minutes,
    bool IsPreview = false,
    string? Body = null,
    string? Url = null,
    string? File = null);

internal sealed record SeedQuestion(string Text, string Explanation, int CorrectIndex, params string[] Options);

internal sealed record SeedQuiz(string Title, string Description, int PassMark, IReadOnlyList<SeedQuestion> Questions);

internal sealed record SeedCourse(
    string Title,
    string Category,
    DifficultyLevel Difficulty,
    int DurationMinutes,
    string Instructor,
    string Cover,
    string ShortDescription,
    string Description,
    string Outcomes,
    bool IsPublished,
    IReadOnlyList<SeedResource> Resources,
    IReadOnlyList<SeedQuiz> Quizzes);

/// <summary>
/// Demonstration content. Lesson text is original; videos are public YouTube lessons (verified embeddable)
/// and links point to official documentation.
/// </summary>
internal static partial class DemoCatalog
{
    public const string Programming = "Programming";
    public const string WebDevelopment = "Web Development";
    public const string Databases = "Databases";
    public const string Cybersecurity = "Cybersecurity";
    public const string Networking = "Networking";
    public const string CloudComputing = "Cloud Computing";

    public static IReadOnlyList<SeedCategory> Categories { get; } =
    [
        new(Programming, "Problem solving and writing clear, testable code in modern languages.", "code-slash"),
        new(WebDevelopment, "Building accessible, secure and responsive websites and web applications.", "globe2"),
        new(Databases, "Designing relational schemas and turning data into information with SQL.", "database"),
        new(Cybersecurity, "Protecting applications, data and people from common attacks.", "shield-lock"),
        new(Networking, "How computers communicate, from cables and switches to internet protocols.", "diagram-3"),
        new(CloudComputing, "Deploying, scaling and operating applications on cloud platforms.", "cloud")
    ];

    public static IReadOnlyList<SeedCourse> Courses { get; } =
    [
        CSharpFundamentals(),
        PythonProblemSolving(),
        ModernHtmlAndCss(),
        JavaScriptEssentials(),
        AspNetCoreMvc(),
        RelationalDatabaseDesign(),
        WebSecurityBasics(),
        NetworkingFundamentals(),
        CloudFoundations(),
        GitForTeams()
    ];

    public static IEnumerable<(string Name, string Email, int DaysAgo)> Learners(string demoStudentEmail) =>
    [
        ("Aisyah Rahman", demoStudentEmail, 75),
        ("Daniel Tan", "daniel.tan@example.com", 88),
        ("Priya Nair", "priya.nair@example.com", 70),
        ("Omar Haddad", "omar.haddad@example.com", 64),
        ("Mei Lin Wong", "meilin.wong@example.com", 52),
        ("Lucas Silva", "lucas.silva@example.com", 45),
        ("Hana Kobayashi", "hana.kobayashi@example.com", 30)
    ];

    /// <summary>The demo student's journey: one finished course, one in progress and one just started.</summary>
    public static IReadOnlyList<(string CourseTitle, double CompletedShare, double QuizSkill)> DemoStudentJourney { get; } =
    [
        ("Modern HTML and CSS", 1.0, 1.0),
        ("ASP.NET Core MVC in Practice", 0.72, 0.5),
        ("Relational Database Design with SQL", 0.2, 0.0)
    ];

    public static IReadOnlyList<(string Name, string Email, string Subject, string Message, int DaysAgo, bool IsRead)> ContactMessages { get; } =
    [
        ("Kavitha Subramaniam", "kavitha.s@example.com", "Certificates for completed courses",
            "Hello LearnHub team, I finished the Modern HTML and CSS course last week. Will there be a way to download a certificate of completion for my portfolio? Thank you.", 6, true),
        ("Jason Lee", "jason.lee@example.com", "Suggestion: a course on mobile development",
            "The courses are very clear, especially the database one. Would you consider adding a course about building mobile apps with .NET MAUI?", 3, false),
        ("Farah Aziz", "farah.aziz@example.com", "Video does not load on campus Wi-Fi",
            "The networking course video does not load on the campus Wi-Fi, but it works on mobile data. The PDF reference opens fine. Could YouTube be blocked on that network?", 1, false)
    ];

    private static string Watch(string videoId) => $"https://www.youtube.com/watch?v={videoId}";
}
