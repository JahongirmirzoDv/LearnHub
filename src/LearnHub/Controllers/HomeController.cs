using LearnHub.Infrastructure;
using LearnHub.Services;
using LearnHub.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnHub.Controllers;

public sealed class HomeController(ICourseCatalogService catalog, IContactService contact) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await catalog.GetHomePageAsync(User.GetUserId(), cancellationToken));

    [HttpGet]
    public IActionResult About() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    public IActionResult Contact()
    {
        var model = new ContactViewModel();
        if (User.Identity?.IsAuthenticated == true)
        {
            model.Name = User.GetDisplayName();
            model.Email = User.Identity.Name ?? string.Empty;
        }

        return View(model);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Forms)]
    public async Task<IActionResult> Contact(ContactViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await contact.SubmitAsync(model, cancellationToken);
        TempData.SetStatus("Message sent. The LearnHub team will reply to your email address.");
        return RedirectToAction(nameof(Contact));
    }
}
