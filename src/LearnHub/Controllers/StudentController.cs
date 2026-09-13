using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class StudentController(IDashboardService dashboards, IEnrollmentService enrollments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        View(await dashboards.GetStudentDashboardAsync(User.GetRequiredUserId(), User.GetDisplayName(), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> MyCourses(MyCoursesFilter filter = MyCoursesFilter.All, CancellationToken cancellationToken = default) =>
        View(await enrollments.GetMyCoursesAsync(User.GetRequiredUserId(), filter, cancellationToken));
}
