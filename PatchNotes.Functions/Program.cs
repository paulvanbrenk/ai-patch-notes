using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatchNotes.Data;
using PatchNotes.Data.AI;
using PatchNotes.Data.GitHub;
using PatchNotes.Sync;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddPatchNotesDbContext(context.Configuration);

        services.AddGitHubClient(options =>
        {
            var token = context.Configuration["GitHub:Token"];
            if (!string.IsNullOrEmpty(token))
                options.Token = token;
        });

        services.AddAiClient(options =>
        {
            var section = context.Configuration.GetSection(AiClientOptions.SectionName);
            var baseUrl = section["BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
                options.BaseUrl = baseUrl;
            options.ApiKey = section["ApiKey"];
            var model = section["Model"];
            if (!string.IsNullOrEmpty(model))
                options.Model = model;
        });

        services.AddTransient<ChangelogResolver>();
        services.AddTransient<VersionGroupingService>();
        services.AddTransient<SyncService>();
        services.AddTransient<SummaryGenerationService>();
        services.AddTransient<SyncPipeline>();
    })
    .Build();

host.Run();
