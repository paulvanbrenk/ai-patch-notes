using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PatchNotes.Data;

public static class DatabaseProviderFactory
{
    private const string ConnectionStringName = "PatchNotes";
    private const string DefaultSqliteConnection = "Data Source=patchnotes.db";

    public static IServiceCollection AddPatchNotesDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? DefaultSqliteConnection;

        if (IsSqlServer(connectionString))
        {
            services.AddDbContext<PatchNotesDbContext, SqlServerContext>(options =>
                ConfigureDbContext(options, connectionString));
        }
        else
        {
            services.AddDbContext<PatchNotesDbContext, SqliteContext>(options =>
                ConfigureDbContext(options, connectionString));
        }

        return services;
    }

    public static void ConfigureDbContext(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        if (IsSqlServer(connectionString))
        {
            // Note: EnableRetryOnFailure is intentionally NOT used here.
            // EF Core's retry strategy buffers query results via BufferedDataReader,
            // which calls SqlDataReader.GetDateTime() on datetimeoffset columns.
            // Microsoft.Data.SqlClient v6 throws InvalidCastException for this.
            // Azure SQL connection-level retry (ConnectRetryCount in connection string)
            // handles transient connection failures without this issue.
            options.UseSqlServer(connectionString);
        }
        else
        {
            options.UseSqlite(connectionString);
        }
    }

    public static bool IsSqlServer(string connectionString)
    {
        // SQL Server connection strings typically contain "Server=" or "Data Source=" with a server name
        // SQLite uses "Data Source=" with a file path ending in .db
        var normalized = connectionString.ToLowerInvariant();

        // Check for SQLite indicators first
        if (normalized.Contains(".db") || normalized.Contains(":memory:"))
        {
            return false;
        }

        // Check for SQL Server indicators
        return normalized.Contains("server=") ||
               normalized.Contains("initial catalog=") ||
               normalized.Contains("database=") ||
               normalized.Contains("tcp:") ||
               normalized.Contains(".database.windows.net");
    }
}
