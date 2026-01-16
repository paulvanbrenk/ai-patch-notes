using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatchNotes.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PatchNotesDbContext>
{
    public PatchNotesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PatchNotesDbContext>();
        optionsBuilder.UseSqlite("Data Source=patchnotes.db");
        return new PatchNotesDbContext(optionsBuilder.Options);
    }
}
