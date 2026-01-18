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
    public DbSet<Notification> Notifications => Set<Notification>();

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

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.Unread);
            entity.HasOne(e => e.Package)
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired(false);
        });
    }
}
