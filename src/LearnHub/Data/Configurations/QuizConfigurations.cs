using LearnHub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

internal sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Quizzes_PassMarkPercent", "PassMarkPercent BETWEEN 0 AND 100"));

        builder.Property(q => q.Title).IsRequired().HasMaxLength(FieldLengths.QuizTitle);
        builder.Property(q => q.Description).HasMaxLength(FieldLengths.QuizDescription);

        builder.HasOne(q => q.Course)
            .WithMany(c => c.Quizzes)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Questions_Points", "Points BETWEEN 1 AND 100"));

        builder.Property(q => q.Text).IsRequired().HasMaxLength(FieldLengths.QuestionText);
        builder.Property(q => q.Explanation).HasMaxLength(FieldLengths.QuestionExplanation);

        builder.HasIndex(q => new { q.QuizId, q.SortOrder });

        builder.HasOne(q => q.Quiz)
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.Property(o => o.Text).IsRequired().HasMaxLength(FieldLengths.AnswerOptionText);

        builder.HasIndex(o => new { o.QuestionId, o.SortOrder });

        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_QuizAttempts_ScorePercent", "ScorePercent BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_QuizAttempts_CorrectCount", "CorrectCount >= 0 AND CorrectCount <= QuestionCount");
            t.HasCheckConstraint("CK_QuizAttempts_Score", "Score >= 0 AND Score <= MaxScore");
        });

        builder.HasIndex(a => new { a.UserId, a.QuizId });
        builder.HasIndex(a => a.CompletedAt);

        builder.HasOne(a => a.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany(u => u.QuizAttempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder)
    {
        builder.HasIndex(a => new { a.QuizAttemptId, a.QuestionId }).IsUnique();

        builder.HasOne(a => a.QuizAttempt)
            .WithMany(attempt => attempt.Answers)
            .HasForeignKey(a => a.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on both links, so editing a quiz can never silently delete a student's answer history.
        // Services clear or delete dependent answers explicitly before removing questions or options.
        builder.HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SelectedOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
