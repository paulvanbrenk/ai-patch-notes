using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatchNotes.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PatchNotesDbContext>
{
    private const string DefaultConnection = "Data Source=patchnotes.db";

    public PatchNotesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatchNotes")
            ?? DefaultConnection;

        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        DatabaseProviderFactory.ConfigureDbContext(optionsBuilder, connectionString);

        return new PatchNotesDbContext(optionsBuilder.Options);
    }
}
