using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Public;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IContactService
{
    Task SubmitAsync(ContactViewModel model, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(CancellationToken cancellationToken = default);

    Task<AdminMessageListViewModel> GetListAsync(AdminMessageQuery query, CancellationToken cancellationToken = default);

    /// <summary>Opens a message and marks it as read.</summary>
    Task<ContactMessageDetails?> OpenAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> SetReadAsync(int id, bool isRead, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ContactService(ApplicationDbContext db, TimeProvider clock, ILogger<ContactService> logger) : IContactService
{
    public const int PageSize = 20;

    public async Task SubmitAsync(ContactViewModel model, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            // Honeypot filled in: behave as if the message was sent, but do not store spam.
            logger.LogInformation("Contact form submission discarded by the spam honeypot.");
            return;
        }

        db.ContactMessages.Add(new ContactMessage
        {
            Name = TextInput.Required(model.Name),
            Email = model.Email.Trim(),
            Subject = TextInput.Required(model.Subject),
            Message = TextInput.Required(model.Message),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Contact message received.");
    }

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) =>
        db.ContactMessages.CountAsync(m => !m.IsRead, cancellationToken);

    public async Task<AdminMessageListViewModel> GetListAsync(AdminMessageQuery query, CancellationToken cancellationToken = default)
    {
        var messages = db.ContactMessages.AsNoTracking();
        if (query.UnreadOnly)
        {
            messages = messages.Where(m => !m.IsRead);
        }

        var results = await messages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ContactMessageListItem(m.Id, m.Name, m.Email, m.Subject, m.IsRead, m.CreatedAt))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        query.Page = results.Page;
        return new AdminMessageListViewModel
        {
            Query = query,
            Results = results,
            UnreadCount = await CountUnreadAsync(cancellationToken)
        };
    }

    public async Task<ContactMessageDetails?> OpenAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await db.SaveChangesAsync(cancellationToken);
        }

        return new ContactMessageDetails(message.Id, message.Name, message.Email, message.Subject, message.Message, message.IsRead, message.CreatedAt);
    }

    public async Task<OperationResult> SetReadAsync(int id, bool isRead, CancellationToken cancellationToken = default)
    {
        var updated = await db.ContactMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsRead, isRead), cancellationToken);
        return updated == 0 ? OperationResult.NotFound() : OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await db.ContactMessages.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return OperationResult.NotFound();
        }

        logger.LogInformation("Contact message {MessageId} deleted.", id);
        return OperationResult.Success();
    }
}
