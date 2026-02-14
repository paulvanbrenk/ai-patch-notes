using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatchNotes.Data;
using PatchNotes.Sync.AI;
using PatchNotes.Sync.GitHub;
using PatchNotes.Sync;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddPatchNotesDbContext(builder.Configuration);

builder.Services.AddGitHubClient(options =>
{
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrEmpty(token))
        options.Token = token;
});

builder.Services.AddAiClient(options =>
{
    var section = builder.Configuration.GetSection(AiClientOptions.SectionName);
    var baseUrl = section["BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
        options.BaseUrl = baseUrl;
    options.ApiKey = section["ApiKey"];
    var model = section["Model"];
    if (!string.IsNullOrEmpty(model))
        options.Model = model;
});

builder.Services.AddTransient<ChangelogResolver>();
builder.Services.AddTransient<VersionGroupingService>();
builder.Services.AddTransient<SyncService>();
builder.Services.AddTransient<SummaryGenerationService>();
builder.Services.AddTransient<SyncPipeline>();

builder.Build().Run();
