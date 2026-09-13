using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

/// <summary>Inbox for messages sent through the public Contact page.</summary>
public sealed class MessagesController(IContactService contact) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminMessageQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await contact.GetListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await contact.OpenAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MarkUnread(int id, CancellationToken cancellationToken)
    {
        var result = await contact.SetReadAsync(id, isRead: false, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Message marked as unread.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await contact.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Message deleted.");
        return RedirectToAction(nameof(Index));
    }
}
