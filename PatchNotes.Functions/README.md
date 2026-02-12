# PatchNotes.Functions

Azure Function App that runs the sync pipeline on a schedule. Deployed to **fn-patchnotes-sync** (Flex Consumption, East US).

## Functions

### SyncReleases

Timer trigger that runs every 6 hours (`0 0 */6 * * *` — midnight, 6am, noon, 6pm UTC).

Executes the `SyncPipeline` which:

1. **Syncs releases** from GitHub for all tracked packages (producer)
2. **Generates AI summaries** for new release groups (consumer)

These run concurrently as a producer-consumer pipeline using `System.Threading.Channels`.

## Configuration

| Setting | Description |
|---|---|
| `ConnectionStrings:PatchNotes` | SQL Server connection string |
| `GitHub:Token` | GitHub PAT for API access |
| `AI:BaseUrl` | AI provider base URL |
| `AI:ApiKey` | AI provider API key |
| `AI:Model` | AI model name (e.g. `gemma3:27b`) |

In Azure, these are set as Function App application settings with `__` as the separator (e.g. `ConnectionStrings__PatchNotes`).

## Local Development

1. Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__PatchNotes": "Data Source=patchnotes.db",
    "GitHub__Token": "<your-github-pat>",
    "AI__ApiKey": "<your-ai-key>",
    "AI__Model": "gemma3:27b"
  }
}
```

2. Run with Azure Functions Core Tools:

```bash
cd PatchNotes.Functions
func start
```

## Infrastructure

- **Plan**: Flex Consumption (Linux)
- **Runtime**: .NET 10 isolated worker
- **Monitoring**: Application Insights (shared with the API)
- **Timeout**: 10 minutes (`host.json`)
- **Deploy**: GitHub Actions pushes to `main` trigger build and deploy via `azure/functions-action@v1`
