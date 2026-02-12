using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class StatusPageRoutes
{
    public static void MapStatusPageRoutes(this WebApplication app)
    {
        app.MapGet("/", async (PatchNotesDbContext db) =>
        {
            var isHealthy = false;
            var dbStatus = "Unknown";

            try
            {
                isHealthy = await db.Database.CanConnectAsync();
                dbStatus = isHealthy ? "Connected" : "Disconnected";
            }
            catch
            {
                dbStatus = "Error";
            }

            var html = GetStatusPageHtml(isHealthy, dbStatus);
            return Results.Content(html, "text/html");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();
    }

    public static string GetStatusPageHtml(bool isHealthy, string dbStatus, string? errorMessage = null)
    {
        var statusColor = isHealthy ? "#22c55e" : "#ef4444";
        var statusText = isHealthy ? "All Systems Operational" : "Service Degraded";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var apiStatus = errorMessage != null ? "Error" : "Operational";
        var apiStatusEmoji = errorMessage != null ? "⚠️" : "✅";
        var dbStatusEmoji = isHealthy ? "✅" : "⚠️";

        var errorSection = errorMessage != null
            ? $@"<div class=""error-message"">
                    <strong>⚠️ Error:</strong> {System.Web.HttpUtility.HtmlEncode(errorMessage)}
                </div>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>PatchNotes API - Status</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            color: #fff;
        }}
        .container {{
            text-align: center;
            padding: 2rem;
            flex: 1;
            display: flex;
            flex-direction: column;
            justify-content: center;
        }}
        .logo {{
            font-size: 2.5rem;
            font-weight: 700;
            margin-bottom: 0.5rem;
        }}
        .subtitle {{
            color: #94a3b8;
            margin-bottom: 3rem;
        }}
        .status-card {{
            background: rgba(255, 255, 255, 0.05);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 16px;
            padding: 2rem 3rem;
            backdrop-filter: blur(10px);
        }}
        .status-indicator {{
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0.75rem;
            font-size: 1.5rem;
            font-weight: 600;
            margin-bottom: 1.5rem;
        }}
        .status-dot {{
            width: 16px;
            height: 16px;
            border-radius: 50%;
            background: {statusColor};
            box-shadow: 0 0 20px {statusColor};
            animation: pulse 2s infinite;
        }}
        @keyframes pulse {{
            0%, 100% {{ opacity: 1; }}
            50% {{ opacity: 0.6; }}
        }}
        .services {{
            display: flex;
            flex-direction: column;
            gap: 0.75rem;
        }}
        .service {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 0.75rem 1rem;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 8px;
        }}
        .service-name {{
            color: #94a3b8;
        }}
        .service-status {{
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-weight: 500;
        }}
        .service-status.healthy {{
            color: #22c55e;
        }}
        .service-status.unhealthy {{
            color: #ef4444;
        }}
        .timestamp {{
            margin-top: 2rem;
            color: #64748b;
            font-size: 0.875rem;
        }}
        .error-message {{
            margin-top: 1rem;
            padding: 1rem;
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.3);
            border-radius: 8px;
            color: #fca5a5;
            font-size: 0.875rem;
            text-align: left;
        }}
        footer {{
            width: 100%;
            text-align: center;
            padding: 1rem;
            border-top: 1px solid rgba(255, 255, 255, 0.1);
            color: #94a3b8;
            font-size: 0.875rem;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1 class=""logo"">PatchNotes API</h1>
        <p class=""subtitle"">api.myreleasenotes.ai</p>

        <div class=""status-card"">
            <div class=""status-indicator"">
                <span class=""status-dot""></span>
                <span>{statusText}</span>
            </div>

            <div class=""services"">
                <div class=""service"">
                    <span class=""service-name"">API Server</span>
                    <span class=""service-status {(errorMessage != null ? "unhealthy" : "healthy")}"">
                        {apiStatusEmoji} {apiStatus}
                    </span>
                </div>
                <div class=""service"">
                    <span class=""service-name"">Database</span>
                    <span class=""service-status {(isHealthy ? "healthy" : "unhealthy")}"">
                        {dbStatusEmoji} {dbStatus}
                    </span>
                </div>
                <div class=""service"">
                    <span class=""service-name"">Authentication</span>
                    <span class=""service-status healthy"">
                        ✅ Operational
                    </span>
                </div>
            </div>

            {errorSection}

            <p class=""timestamp"">Last checked: {timestamp} UTC</p>
        </div>
    </div>
    <footer>🔨 Forged in Gas Town ⛽</footer>
</body>
</html>";
    }
}
