using Microsoft.EntityFrameworkCore;

namespace PatchNotes.Data;

public class PatchNotesDbContext : DbContext
{
    public PatchNotesDbContext(DbContextOptions<PatchNotesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Release> Releases => Set<Release>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasIndex(e => e.NpmName);
        });

        modelBuilder.Entity<Release>(entity =>
        {
            entity.HasIndex(e => e.PublishedAt);
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Releases)
                .HasForeignKey(e => e.PackageId);
        });
    }
}
