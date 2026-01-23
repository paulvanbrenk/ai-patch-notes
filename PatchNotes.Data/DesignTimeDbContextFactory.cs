using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatchNotes.Data;

/// <summary>
/// SQLite context for local development.
/// Migrations: dotnet ef migrations add MigrationName --context SqliteContext --output-dir Migrations/Sqlite --startup-project ../PatchNotes.Api
/// </summary>
public class SqliteContext : PatchNotesDbContext
{
    public SqliteContext(DbContextOptions<PatchNotesDbContext> options) : base(options) { }
}

/// <summary>
/// SQL Server context for production.
/// Migrations: dotnet ef migrations add MigrationName --context SqlServerContext --output-dir Migrations/SqlServer --startup-project ../PatchNotes.Api
/// </summary>
public class SqlServerContext : PatchNotesDbContext
{
    public SqlServerContext(DbContextOptions<PatchNotesDbContext> options) : base(options) { }
}

/// <summary>
/// Design-time factory for SQLite migrations.
/// </summary>
public class SqliteContextFactory : IDesignTimeDbContextFactory<SqliteContext>
{
    public SqliteContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        optionsBuilder.UseSqlite("Data Source=patchnotes.db");
        return new SqliteContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Design-time factory for SQL Server migrations.
/// Set ConnectionStrings__PatchNotes environment variable.
/// </summary>
public class SqlServerContextFactory : IDesignTimeDbContextFactory<SqlServerContext>
{
    public SqlServerContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatchNotes") ??
            throw new InvalidOperationException(
                "Connection string not found. " +
                "Set ConnectionStrings__PatchNotes environment variable.");

        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        optionsBuilder.UseSqlServer(connectionString, options =>
            options.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null));

        return new SqlServerContext(optionsBuilder.Options);
    }
}

// Keep backward compatibility for existing migrations
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PatchNotesDbContext>
{
    public PatchNotesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatchNotes")
            ?? "Data Source=patchnotes.db";

        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        DatabaseProviderFactory.ConfigureDbContext(optionsBuilder, connectionString);

        return new PatchNotesDbContext(optionsBuilder.Options);
    }
}
