using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatchNotes.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PatchNotesDbContext>
{
    // Use a dummy SQL Server connection string for migration generation
    // This ensures migrations are generated with SQL Server column types
    // which are required for Azure SQL production
    private const string DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=PatchNotes;Trusted_Connection=True;";

    public PatchNotesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatchNotes")
            ?? DefaultConnection;

        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        DatabaseProviderFactory.ConfigureDbContext(optionsBuilder, connectionString);

        return new PatchNotesDbContext(optionsBuilder.Options);
    }
}
