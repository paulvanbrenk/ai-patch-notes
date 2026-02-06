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

var missingStytchKeys = new List<string>();
if (string.IsNullOrEmpty(stytchProjectId)) missingStytchKeys.Add("Stytch:ProjectId");
if (string.IsNullOrEmpty(stytchSecret)) missingStytchKeys.Add("Stytch:Secret");
if (string.IsNullOrEmpty(stytchWebhookSecret)) missingStytchKeys.Add("Stytch:WebhookSecret");

if (missingStytchKeys.Count > 0)
{
    throw new InvalidOperationException(
        $"Missing required Stytch configuration: {string.Join(", ", missingStytchKeys)}. " +
        "Please configure these values in appsettings.json or environment variables.");
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

builder.Services.AddSingleton<IStytchClient, StytchClient>();

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
else
{
    // In production, show error status page for unhandled exceptions
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";

            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var errorMessage = exceptionFeature?.Error?.Message ?? "An unexpected error occurred";

            var html = StatusPageRoutes.GetStatusPageHtml(false, "Unknown", errorMessage);
            await context.Response.WriteAsync(html);
        });
    });
}

app.UseCors();

// Map routes
app.MapStatusPageRoutes();
app.MapPackageRoutes();
app.MapReleaseRoutes();
app.MapNotificationRoutes();
app.MapUserRoutes();
app.MapStytchWebhook();

app.Run();

// Make the Program class accessible to the test project
public partial class Program { }
