using LearnHub.Data;
using LearnHub.Data.Seed;
using LearnHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLearnHubDatabase(builder.Configuration, builder.Environment)
    .AddLearnHubIdentity(builder.Environment)
    .AddLearnHubApplicationServices(builder.Configuration)
    .AddLearnHubWeb(builder.Configuration, builder.Environment);

var app = builder.Build();

// HTTP request pipeline – order matters.
if (!app.Environment.IsDevelopment())
{
    // Friendly error page instead of a stack trace; HSTS tells browsers to always use HTTPS.
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseLearnHubStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapLearnHubRoutes();

await app.InitializeDatabaseAsync();
await app.RunAsync();
