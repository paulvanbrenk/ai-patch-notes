using PatchNotes.Data;
using PatchNotes.Data.GitHub;
using PatchNotes.Data.AI;
using PatchNotes.Data.Stytch;
using PatchNotes.Sync;
using PatchNotes.Api.Routes;
using PatchNotes.Api.Webhooks;

var builder = WebApplication.CreateBuilder(args);

// Stytch configuration
var stytchProjectId = builder.Configuration["Stytch:ProjectId"];
var stytchSecret = builder.Configuration["Stytch:Secret"];
var stytchWebhookSecret = builder.Configuration["Stytch:WebhookSecret"];

if (string.IsNullOrEmpty(stytchProjectId) || string.IsNullOrEmpty(stytchSecret))
{
    Console.WriteLine("WARNING: Stytch:ProjectId and Stytch:Secret are not configured. Authentication will not work.");
}

builder.Services.AddOpenApi();
builder.Services.AddPatchNotesDbContext(builder.Configuration);
builder.Services.AddHttpClient();

builder.Services.AddGitHubClient(options =>
{
    options.Token = builder.Configuration["GitHub:Token"];
});

builder.Services.AddAiClient(options =>
{
    options.ApiKey = builder.Configuration["AI:ApiKey"];
    var baseUrl = builder.Configuration["AI:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        options.BaseUrl = baseUrl;
    }
    var model = builder.Configuration["AI:Model"];
    if (!string.IsNullOrEmpty(model))
    {
        options.Model = model;
    }
});

builder.Services.AddStytchClient(options =>
{
    options.ProjectId = stytchProjectId;
    options.Secret = stytchSecret;
    options.WebhookSecret = stytchWebhookSecret;
    var baseUrl = builder.Configuration["Stytch:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        options.BaseUrl = baseUrl;
    }
});

builder.Services.AddScoped<SyncService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                  "https://app.mypkgupdate.com",
                  "http://localhost:5173",
                  "http://localhost:3000")
              .WithHeaders("Content-Type", "X-API-Key", "Accept")
              .WithMethods("GET", "POST", "PATCH", "DELETE")
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Map routes
app.MapPackageRoutes();
app.MapReleaseRoutes();
app.MapNotificationRoutes();
app.MapUserRoutes();
app.MapStytchWebhook();

app.Run();

// Make the Program class accessible to the test project
public partial class Program { }
