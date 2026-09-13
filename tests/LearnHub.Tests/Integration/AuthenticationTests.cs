using System.Net;
using LearnHub.Models;
using LearnHub.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Integration;

public sealed class AuthenticationTests(LearnHubWebApplicationFactory factory) : IClassFixture<LearnHubWebApplicationFactory>
{
    [Fact]
    public async Task Registration_creates_a_student_signs_them_in_and_hashes_the_password()
    {
        var client = factory.CreateBrowserClient();

        var email = await client.RegisterStudentAsync("Test Student");

        var dashboard = await client.GetHtmlAsync("/Student/Dashboard");
        Assert.Contains("Welcome back, Test", dashboard);

        var (isStudent, passwordHash) = await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == email);
            var studentRoleId = await db.Roles.Where(r => r.Name == AppRoles.Student).Select(r => r.Id).SingleAsync();
            var hasRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == studentRoleId);
            return (hasRole, user.PasswordHash);
        });
        Assert.True(isStudent);
        Assert.NotNull(passwordHash);
        Assert.DoesNotContain(LearnHubWebApplicationFactory.NewUserPassword, passwordHash);
    }

    [Fact]
    public async Task Invalid_registration_is_rejected_by_server_side_validation()
    {
        var client = factory.CreateBrowserClient();
        var email = $"invalid-{Guid.NewGuid():N}@learnhub.test";

        var response = await client.SubmitFormAsync("/Account/Register", "/Account/Register", new Dictionary<string, string>
        {
            ["FullName"] = "Test Student",
            ["Email"] = email,
            ["Password"] = LearnHubWebApplicationFactory.NewUserPassword,
            ["ConfirmPassword"] = "Something-Else-1",
            ["AcceptTerms"] = "true"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The passwords do not match.", await response.Content.ReadAsStringAsync());
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == email)));
    }

    [Fact]
    public async Task Registering_with_an_existing_email_shows_a_single_clear_error()
    {
        var response = await factory.CreateBrowserClient().SubmitFormAsync("/Account/Register", "/Account/Register", new Dictionary<string, string>
        {
            ["FullName"] = "Copy Cat",
            ["Email"] = LearnHubWebApplicationFactory.DemoStudentEmail,
            ["Password"] = LearnHubWebApplicationFactory.NewUserPassword,
            ["ConfirmPassword"] = LearnHubWebApplicationFactory.NewUserPassword,
            ["AcceptTerms"] = "true"
        });

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("An account with this email address already exists.", html);
    }

    [Fact]
    public async Task Students_and_admins_are_sent_to_their_own_dashboards_after_login()
    {
        var student = await factory.CreateBrowserClient().LoginAsync(
            LearnHubWebApplicationFactory.DemoStudentEmail, LearnHubWebApplicationFactory.DemoStudentPassword);
        Assert.Equal("/Student/Dashboard", student.LocationPath());

        var admin = await factory.CreateBrowserClient().LoginAsync(
            LearnHubWebApplicationFactory.AdminEmail, LearnHubWebApplicationFactory.AdminPassword);
        Assert.Equal("/Admin", admin.LocationPath());
    }

    [Fact]
    public async Task Wrong_password_shows_a_generic_error()
    {
        var response = await factory.CreateBrowserClient().LoginAsync(LearnHubWebApplicationFactory.DemoStudentEmail, "Wrong-Password-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The email address or password is incorrect.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_redirects_back_to_a_local_return_url_but_never_to_another_site()
    {
        var local = await factory.CreateBrowserClient().SubmitFormAsync("/Account/Login", "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = LearnHubWebApplicationFactory.DemoStudentEmail,
            ["Password"] = LearnHubWebApplicationFactory.DemoStudentPassword,
            ["ReturnUrl"] = "/Student/MyCourses"
        });
        Assert.Equal("/Student/MyCourses", local.LocationPath());

        var external = await factory.CreateBrowserClient().SubmitFormAsync("/Account/Login", "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = LearnHubWebApplicationFactory.DemoStudentEmail,
            ["Password"] = LearnHubWebApplicationFactory.DemoStudentPassword,
            ["ReturnUrl"] = "https://evil.example/phishing"
        });
        Assert.Equal("/Student/Dashboard", external.LocationPath());
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = factory.CreateBrowserClient();
        await client.LoginAsDemoStudentAsync();

        var logout = await client.SubmitFormAsync("/Student/Dashboard", "/Account/Logout", []);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

        var dashboard = await client.GetAsync("/Student/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
        Assert.StartsWith("/Account/Login", dashboard.LocationPath());
    }

    [Fact]
    public async Task Account_is_locked_after_five_failed_attempts()
    {
        var email = await factory.CreateBrowserClient().RegisterStudentAsync("Locked Student");
        var attacker = factory.CreateBrowserClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await attacker.LoginAsync(email, "Wrong-Password-1");
        }

        var response = await attacker.LoginAsync(email, LearnHubWebApplicationFactory.NewUserPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This account is locked.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Posts_without_an_anti_forgery_token_are_rejected()
    {
        var response = await factory.CreateBrowserClient().PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = LearnHubWebApplicationFactory.DemoStudentEmail,
            ["Password"] = LearnHubWebApplicationFactory.DemoStudentPassword
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
