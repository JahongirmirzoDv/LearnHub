using LearnHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

/// <summary>Public list of course categories; each links to the course catalogue filtered by that category.</summary>
public sealed class CategoriesController(ICategoryService categories) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await categories.GetPublicCategoriesAsync(cancellationToken));
}
