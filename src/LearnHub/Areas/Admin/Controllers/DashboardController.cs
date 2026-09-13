using LearnHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class DashboardController(IDashboardService dashboards) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboards.GetAdminDashboardAsync(cancellationToken));
}
