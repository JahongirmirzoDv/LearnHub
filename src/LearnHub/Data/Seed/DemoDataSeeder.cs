using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Services.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnHub.Data.Seed;

/// <summary>
/// Seeds an empty database with the demonstration catalogue from <see cref="DemoCatalog"/> and a small,
/// deterministic set of learners and learning activity, so dashboards show realistic data. Runs only when
/// no categories exist, so it never duplicates or overwrites real data.
/// </summary>
public sealed class DemoDataSeeder(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IFileStorageService storage,
    IHostEnvironment environment,
    IOptions<SeedOptions> seedOptions,
    TimeProvider clock,
    ILogger<DemoDataSeeder> logger)
{
    private const int RandomSeed = 2026;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        logger.LogInformation("Seeding demonstration catalogue and learners.");

        var courses = await SeedCatalogueAsync(now, cancellationToken);
        var learners = await SeedLearnersAsync(now);
        await SeedActivityAsync(courses, learners, now, cancellationToken);
        SeedContactMessages(now);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo data seeded: {CourseCount} courses, {LearnerCount} learners.", courses.Count, learners.Count);
    }

    private async Task<List<Course>> SeedCatalogueAsync(DateTime now, CancellationToken cancellationToken)
    {
        var categories = DemoCatalog.Categories.ToDictionary(
            c => c.Name,
            c => new Category { Name = c.Name, Description = c.Description, IconName = c.Icon, CreatedAt = now.AddDays(-150) });
        db.Categories.AddRange(categories.Values);

        var courses = new List<Course>();
        for (var index = 0; index < DemoCatalog.Courses.Count; index++)
        {
            var definition = DemoCatalog.Courses[index];
            var created = now.AddDays(-120 + (index * 9));
            var course = new Course
            {
                Title = definition.Title,
                ShortDescription = definition.ShortDescription,
                Description = definition.Description,
                LearningOutcomes = definition.Outcomes,
                InstructorName = definition.Instructor,
                Difficulty = definition.Difficulty,
                DurationMinutes = definition.DurationMinutes,
                ThumbnailPath = $"/images/courses/{definition.Cover}.svg",
                IsPublished = definition.IsPublished,
                Category = categories[definition.Category],
                CreatedAt = created,
                UpdatedAt = created.AddDays(3)
            };

            var order = 1;
            foreach (var item in definition.Resources)
            {
                var resource = new LearningResource
                {
                    Title = item.Title,
                    Summary = item.Summary,
                    Type = item.Type,
                    Body = item.Body,
                    ExternalUrl = item.Url,
                    EstimatedMinutes = item.Minutes,
                    IsPreview = item.IsPreview,
                    SortOrder = order++,
                    CreatedAt = created,
                    UpdatedAt = created
                };

                if (item.File is not null && !await AttachSeedFileAsync(resource, item.File, cancellationToken))
                {
                    continue;
                }

                course.Resources.Add(resource);
            }

            foreach (var quizDefinition in definition.Quizzes)
            {
                var quiz = new Quiz
                {
                    Title = quizDefinition.Title,
                    Description = quizDefinition.Description,
                    PassMarkPercent = quizDefinition.PassMark,
                    IsPublished = definition.IsPublished,
                    CreatedAt = created,
                    UpdatedAt = created
                };

                var questionOrder = 1;
                foreach (var questionDefinition in quizDefinition.Questions)
                {
                    var question = new Question { Text = questionDefinition.Text, Explanation = questionDefinition.Explanation, SortOrder = questionOrder++ };
                    for (var optionIndex = 0; optionIndex < questionDefinition.Options.Length; optionIndex++)
                    {
                        question.Options.Add(new AnswerOption
                        {
                            Text = questionDefinition.Options[optionIndex],
                            IsCorrect = optionIndex == questionDefinition.CorrectIndex,
                            SortOrder = optionIndex
                        });
                    }

                    quiz.Questions.Add(question);
                }

                course.Quizzes.Add(quiz);
            }

            courses.Add(course);
        }

        db.Courses.AddRange(courses);
        await db.SaveChangesAsync(cancellationToken);
        return courses;
    }

    /// <summary>Copies a bundled document or image into private storage, exactly as an admin upload would be stored.</summary>
    private async Task<bool> AttachSeedFileAsync(LearningResource resource, string fileName, CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(environment.ContentRootPath, "Data", "Seed", "Files", fileName);
        if (!File.Exists(sourcePath))
        {
            logger.LogWarning("Seed file {FileName} is missing; resource \"{Title}\" was skipped.", fileName, resource.Title);
            return false;
        }

        var kind = resource.Type == ResourceType.Pdf ? UploadKind.ResourceDocument : UploadKind.ResourceImage;
        await using var stream = File.OpenRead(sourcePath);
        var saved = await storage.SaveAsync(stream, fileName, stream.Length, kind, cancellationToken);
        if (!saved.Succeeded)
        {
            logger.LogWarning("Seed file {FileName} was rejected: {Error}", fileName, saved.Error);
            return false;
        }

        resource.FilePath = saved.Value!.RelativePath;
        resource.FileName = saved.Value.FileName;
        resource.FileContentType = saved.Value.ContentType;
        resource.FileSizeBytes = saved.Value.SizeBytes;
        return true;
    }

    private async Task<List<ApplicationUser>> SeedLearnersAsync(DateTime now)
    {
        var options = seedOptions.Value;
        var learners = new List<ApplicationUser>();

        foreach (var (name, email, daysAgo) in DemoCatalog.Learners(options.DemoStudentEmail))
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = name,
                CreatedAt = now.AddDays(-daysAgo)
            };

            // Only the demo student can sign in (when a password is configured); other learners exist to make the data realistic.
            var isDemoStudent = email == options.DemoStudentEmail;
            var result = isDemoStudent && !string.IsNullOrWhiteSpace(options.DemoStudentPassword)
                ? await userManager.CreateAsync(user, options.DemoStudentPassword)
                : await userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                logger.LogWarning("Demo learner {Email} was not created: {Errors}", email, string.Join(" ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, AppRoles.Student);
            learners.Add(user);
        }

        return learners;
    }

    private async Task SeedActivityAsync(List<Course> courses, List<ApplicationUser> learners, DateTime now, CancellationToken cancellationToken)
    {
        var random = new Random(RandomSeed);
        var published = courses.Where(c => c.IsPublished).ToList();
        var demoEmail = seedOptions.Value.DemoStudentEmail;

        foreach (var learner in learners)
        {
            // The demo student gets a hand-picked journey: one finished course, one in progress, one just started.
            var plan = learner.Email == demoEmail
                ? DemoCatalog.DemoStudentJourney
                    .Select(step => (Course: published.First(c => c.Title == step.CourseTitle), step.CompletedShare, step.QuizSkill))
                    .ToList()
                : published
                    .OrderBy(_ => random.Next())
                    .Take(random.Next(2, 5))
                    .Select(course => (Course: course, CompletedShare: random.NextDouble(), QuizSkill: 0.45 + (random.NextDouble() * 0.5)))
                    .ToList();

            var cursor = learner.CreatedAt.AddDays(1);
            foreach (var (course, completedShare, quizSkill) in plan)
            {
                var enrolledAt = Later(ref cursor, random, now, maxDays: 6);
                var enrollment = new Enrollment { UserId = learner.Id, CourseId = course.Id, EnrolledAt = enrolledAt, LastAccessedAt = enrolledAt };
                db.Enrollments.Add(enrollment);

                var resources = course.Resources.OrderBy(r => r.SortOrder).ToList();
                var completedCount = (int)Math.Round(resources.Count * completedShare);
                foreach (var resource in resources.Take(completedCount))
                {
                    var completedAt = Later(ref cursor, random, now, maxDays: 2);
                    db.ResourceCompletions.Add(new ResourceCompletion { UserId = learner.Id, LearningResourceId = resource.Id, CompletedAt = completedAt });
                    enrollment.LastAccessedAt = completedAt;
                }

                // Learners attempt the quiz once they have worked through most of the course.
                if (completedShare >= 0.6)
                {
                    foreach (var quiz in course.Quizzes)
                    {
                        var submittedAt = Later(ref cursor, random, now, maxDays: 2);
                        db.QuizAttempts.Add(CreateAttempt(quiz, learner.Id, quizSkill, submittedAt, random));
                        enrollment.LastAccessedAt = submittedAt;
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static QuizAttempt CreateAttempt(Quiz quiz, string userId, double skill, DateTime submittedAt, Random random)
    {
        var questions = quiz.Questions
            .Select(q => new GradingQuestion(q.Id, q.Options.Select(o => new GradingOption(o.Id, o.IsCorrect)).ToList()))
            .ToList();

        var answers = new Dictionary<int, int>();
        foreach (var question in quiz.Questions)
        {
            var options = question.Options.ToList();
            var chosen = random.NextDouble() < skill
                ? options.First(o => o.IsCorrect)
                : options[random.Next(options.Count)];
            answers[question.Id] = chosen.Id;
        }

        var graded = QuizGrader.Grade(questions, answers, quiz.PassMarkPercent);
        return new QuizAttempt
        {
            QuizId = quiz.Id,
            UserId = userId,
            SubmittedAt = submittedAt,
            CorrectCount = graded.CorrectCount,
            QuestionCount = graded.QuestionCount,
            ScorePercent = graded.ScorePercent,
            Passed = graded.Passed,
            Answers = graded.Answers
                .Select(a => new QuizAnswer { QuestionId = a.QuestionId, SelectedOptionId = a.SelectedOptionId, IsCorrect = a.IsCorrect })
                .ToList()
        };
    }

    private void SeedContactMessages(DateTime now)
    {
        foreach (var (name, email, subject, message, daysAgo, isRead) in DemoCatalog.ContactMessages)
        {
            db.ContactMessages.Add(new ContactMessage
            {
                Name = name,
                Email = email,
                Subject = subject,
                Message = message,
                IsRead = isRead,
                CreatedAt = now.AddDays(-daysAgo)
            });
        }
    }

    /// <summary>Moves a timeline cursor forward by a random amount; it approaches but never passes the present.</summary>
    private static DateTime Later(ref DateTime cursor, Random random, DateTime now, int maxDays)
    {
        var next = cursor.AddHours(random.Next(3, Math.Max(4, maxDays * 24)));
        if (next >= now)
        {
            next = cursor + ((now - cursor) / 2);
        }

        cursor = next;
        return next;
    }
}
