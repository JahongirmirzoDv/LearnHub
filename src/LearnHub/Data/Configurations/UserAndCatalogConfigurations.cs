using LearnHub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(FieldLengths.PersonName);
        builder.Property(u => u.Bio).HasMaxLength(FieldLengths.Bio);
        builder.Ignore(u => u.IsDeactivated);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(FieldLengths.CategoryName);
        builder.Property(c => c.Description).HasMaxLength(FieldLengths.CategoryDescription);
        builder.Property(c => c.IconName).IsRequired().HasMaxLength(FieldLengths.IconName);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Courses_DurationMinutes", "DurationMinutes > 0"));

        builder.Property(c => c.Title).IsRequired().HasMaxLength(FieldLengths.CourseTitle);
        builder.Property(c => c.ShortDescription).IsRequired().HasMaxLength(FieldLengths.CourseShortDescription);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(FieldLengths.CourseDescription);
        builder.Property(c => c.LearningOutcomes).HasMaxLength(FieldLengths.CourseLearningOutcomes);
        builder.Property(c => c.InstructorName).IsRequired().HasMaxLength(FieldLengths.PersonName);
        builder.Property(c => c.ThumbnailPath).HasMaxLength(FieldLengths.WebPath);

        // Stored as text ("Beginner") so the database is readable without the C# enum.
        builder.Property(c => c.Difficulty).HasConversion<string>().HasMaxLength(FieldLengths.EnumName);

        builder.HasIndex(c => c.Title);
        builder.HasIndex(c => new { c.IsPublished, c.CategoryId });

        // Restrict: a category that still has courses cannot be deleted.
        builder.HasOne(c => c.Category)
            .WithMany(category => category.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LearningResourceConfiguration : IEntityTypeConfiguration<LearningResource>
{
    public void Configure(EntityTypeBuilder<LearningResource> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_LearningResources_SortOrder", "SortOrder >= 0"));

        builder.Property(r => r.Title).IsRequired().HasMaxLength(FieldLengths.ResourceTitle);
        builder.Property(r => r.Summary).HasMaxLength(FieldLengths.ResourceSummary);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(FieldLengths.EnumName);
        builder.Property(r => r.Body).HasMaxLength(FieldLengths.ResourceBody);
        builder.Property(r => r.Solution).HasMaxLength(FieldLengths.ResourceBody);
        builder.Property(r => r.ExternalUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(r => r.FilePath).HasMaxLength(FieldLengths.WebPath);
        builder.Property(r => r.FileName).HasMaxLength(FieldLengths.FileName);
        builder.Property(r => r.FileContentType).HasMaxLength(FieldLengths.ContentType);

        builder.HasIndex(r => new { r.CourseId, r.SortOrder });
        builder.HasIndex(r => new { r.CourseId, r.IsPublished });

        builder.HasOne(r => r.Course)
            .WithMany(c => c.Resources)
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Enrollments_CompletionPercentage", "CompletionPercentage BETWEEN 0 AND 100"));

        // A student can enrol in a course only once.
        builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
        builder.HasIndex(e => new { e.CourseId, e.CompletionPercentage });

        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ResourceCompletionConfiguration : IEntityTypeConfiguration<ResourceCompletion>
{
    public void Configure(EntityTypeBuilder<ResourceCompletion> builder)
    {
        builder.HasIndex(c => new { c.UserId, c.LearningResourceId }).IsUnique();

        builder.HasOne(c => c.User)
            .WithMany(u => u.ResourceCompletions)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.LearningResource)
            .WithMany(r => r.Completions)
            .HasForeignKey(c => c.LearningResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(m => m.Name).IsRequired().HasMaxLength(FieldLengths.PersonName);
        builder.Property(m => m.Email).IsRequired().HasMaxLength(FieldLengths.Email);
        builder.Property(m => m.Subject).IsRequired().HasMaxLength(FieldLengths.ContactSubject);
        builder.Property(m => m.Message).IsRequired().HasMaxLength(FieldLengths.ContactMessage);

        builder.HasIndex(m => new { m.IsRead, m.CreatedAt });
    }
}
