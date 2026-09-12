using LearnHub.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLearnHubDatabase(builder.Configuration, builder.Environment);
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.MapGet("/", () => "LearnHub is starting up.");

app.Run();
