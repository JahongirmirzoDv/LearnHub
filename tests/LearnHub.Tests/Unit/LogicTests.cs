using System.ComponentModel.DataAnnotations;
using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Account;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Shared;

namespace LearnHub.Tests.Unit;

public sealed class QuizGraderTests
{
    private static readonly IReadOnlyList<GradingQuestion> Questions =
    [
        new(1, 1, [new GradingOption(11, true), new GradingOption(12, false)]),
        new(2, 1, [new GradingOption(21, false), new GradingOption(22, true)]),
        new(3, 1, [new GradingOption(31, true), new GradingOption(32, false)])
    ];

    [Fact]
    public void Scores_correct_answers_and_applies_the_pass_mark()
    {
        var result = QuizGrader.Grade(Questions, new Dictionary<int, int> { [1] = 11, [2] = 22, [3] = 32 }, passMarkPercent: 60);

        Assert.Equal(2, result.CorrectCount);
        Assert.Equal(3, result.QuestionCount);
        Assert.Equal(66, result.ScorePercent); // floor(66.67)
        Assert.True(result.Passed);
    }

    [Fact]
    public void Score_is_rounded_down_so_the_pass_mark_is_never_reached_by_rounding()
    {
        var result = QuizGrader.Grade(Questions, new Dictionary<int, int> { [1] = 11, [2] = 22 }, passMarkPercent: 67);

        Assert.Equal(66, result.ScorePercent);
        Assert.False(result.Passed);
    }

    [Fact]
    public void Questions_are_weighted_by_their_points()
    {
        IReadOnlyList<GradingQuestion> weighted =
        [
            new(1, 1, [new GradingOption(11, true), new GradingOption(12, false)]),
            new(2, 3, [new GradingOption(21, true), new GradingOption(22, false)])
        ];

        var onlyHeavy = QuizGrader.Grade(weighted, new Dictionary<int, int> { [1] = 12, [2] = 21 }, passMarkPercent: 70);
        var onlyLight = QuizGrader.Grade(weighted, new Dictionary<int, int> { [1] = 11, [2] = 22 }, passMarkPercent: 70);

        Assert.Equal((3, 4, 75, true), (onlyHeavy.Score, onlyHeavy.MaxScore, onlyHeavy.ScorePercent, onlyHeavy.Passed));
        Assert.Equal((1, 4, 25, false), (onlyLight.Score, onlyLight.MaxScore, onlyLight.ScorePercent, onlyLight.Passed));
        Assert.Equal(1, onlyHeavy.CorrectCount);
    }

    [Fact]
    public void Unanswered_questions_count_as_incorrect()
    {
        var result = QuizGrader.Grade(Questions, new Dictionary<int, int>(), passMarkPercent: 50);

        Assert.Equal(0, result.CorrectCount);
        Assert.All(result.Answers, answer => Assert.Null(answer.SelectedOptionId));
        Assert.False(result.Passed);
    }

    [Fact]
    public void An_option_id_from_another_question_is_ignored()
    {
        // Tampered form: question 1 "answered" with the correct option of question 2.
        var result = QuizGrader.Grade(Questions, new Dictionary<int, int> { [1] = 22 }, passMarkPercent: 50);

        var first = result.Answers.Single(a => a.QuestionId == 1);
        Assert.Null(first.SelectedOptionId);
        Assert.False(first.IsCorrect);
    }
}

public sealed class DisplayFormatTests
{
    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(135, "2 h 15 min")]
    [InlineData(0, "—")]
    public void Duration_is_human_readable(int minutes, string expected) =>
        Assert.Equal(expected, DisplayFormat.Duration(minutes));

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2 KB")]
    [InlineData(1_572_864, "1.5 MB")]
    public void File_size_is_human_readable(long bytes, string expected) =>
        Assert.Equal(expected, DisplayFormat.FileSize(bytes));

    [Theory]
    [InlineData(0, "pw-0")]
    [InlineData(1, "pw-5")]
    [InlineData(42, "pw-40")]
    [InlineData(43, "pw-45")]
    [InlineData(100, "pw-100")]
    [InlineData(250, "pw-100")]
    public void Progress_width_class_is_stepped_and_shows_any_started_progress(int percent, string expected) =>
        Assert.Equal(expected, DisplayFormat.ProgressWidthClass(percent));

    [Theory]
    [InlineData("Aisyah Rahman", "AR")]
    [InlineData("hana", "H")]
    [InlineData("  ", "?")]
    public void Initials_come_from_first_and_last_name(string name, string expected) =>
        Assert.Equal(expected, DisplayFormat.Initials(name));

    [Fact]
    public void Category_line_class_cycles_through_eight_colours()
    {
        Assert.Equal("line-1", DisplayFormat.LineClass(1));
        Assert.Equal("line-0", DisplayFormat.LineClass(8));
        Assert.Equal("line-1", DisplayFormat.LineClass(9));
    }
}

public sealed class CourseProgressTests
{
    [Fact]
    public void Percent_and_completion_are_derived_from_counts()
    {
        Assert.Equal(0, CourseProgress.Empty.Percent);
        Assert.False(CourseProgress.Empty.IsCompleted);

        var partial = new CourseProgress(2, 3);
        Assert.Equal(66, partial.Percent);
        Assert.True(partial.IsStarted);
        Assert.False(partial.IsCompleted);

        Assert.True(new CourseProgress(3, 3).IsCompleted);
    }
}

public sealed class ViewModelValidationTests
{
    [Fact]
    public void Register_requires_matching_strong_passwords_and_accepted_terms()
    {
        var model = new RegisterViewModel
        {
            FullName = "A",
            Email = "not-an-email",
            Password = "weakpass",
            ConfirmPassword = "different",
            AcceptTerms = false
        };

        var errors = Validate(model);

        Assert.Contains(nameof(RegisterViewModel.FullName), errors);
        Assert.Contains(nameof(RegisterViewModel.Email), errors);
        Assert.Contains(nameof(RegisterViewModel.Password), errors);
        Assert.Contains(nameof(RegisterViewModel.ConfirmPassword), errors);
        Assert.Contains(nameof(RegisterViewModel.AcceptTerms), errors);
    }

    [Fact]
    public void A_valid_registration_has_no_errors()
    {
        var model = new RegisterViewModel
        {
            FullName = "Aisyah Rahman",
            Email = "aisyah@example.com",
            Password = "Learning2026",
            ConfirmPassword = "Learning2026",
            AcceptTerms = true
        };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Question_needs_two_answers_one_marked_correct_and_no_duplicates()
    {
        var tooFew = new QuestionFormViewModel { Text = "What is SQL?", Options = [new() { Text = "Only one" }], CorrectOptionIndex = 0 };
        Assert.Contains(nameof(QuestionFormViewModel.Options), Validate(tooFew));

        var correctPointsAtEmptyRow = new QuestionFormViewModel
        {
            Text = "What is SQL?",
            Options = [new() { Text = "A language" }, new() { Text = "A database" }, new() { Text = "" }],
            CorrectOptionIndex = 2
        };
        Assert.Contains(nameof(QuestionFormViewModel.CorrectOptionIndex), Validate(correctPointsAtEmptyRow));

        var duplicates = new QuestionFormViewModel
        {
            Text = "What is SQL?",
            Options = [new() { Text = "Same" }, new() { Text = "same" }],
            CorrectOptionIndex = 0
        };
        Assert.Contains(nameof(QuestionFormViewModel.Options), Validate(duplicates));

        var valid = new QuestionFormViewModel
        {
            Text = "What is SQL?",
            Options = [new() { Text = "A query language" }, new() { Text = "A web server" }],
            CorrectOptionIndex = 0
        };
        Assert.Empty(Validate(valid));
    }

    [Theory]
    [InlineData(ResourceType.Article, null, null, nameof(ResourceFormViewModel.Body))]
    [InlineData(ResourceType.Video, null, "https://example.com/video", nameof(ResourceFormViewModel.ExternalUrl))]
    [InlineData(ResourceType.Link, null, "http://insecure.example.com", nameof(ResourceFormViewModel.ExternalUrl))]
    [InlineData(ResourceType.Image, null, null, nameof(ResourceFormViewModel.Summary))]
    [InlineData(ResourceType.Pdf, null, null, nameof(ResourceFormViewModel.UploadFile))]
    public void Resource_rules_depend_on_the_resource_type(ResourceType type, string? body, string? url, string expectedField)
    {
        var model = new ResourceFormViewModel { CourseId = 1, Title = "A resource", Type = type, Body = body, ExternalUrl = url };
        Assert.Contains(expectedField, Validate(model));
    }

    [Fact]
    public void Category_icon_must_come_from_the_allow_list()
    {
        var model = new CategoryFormViewModel { Name = "Design", IconName = "x\" onmouseover=\"alert(1)" };
        Assert.Contains(nameof(CategoryFormViewModel.IconName), Validate(model));
    }

    private static List<string> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToList();
    }
}
