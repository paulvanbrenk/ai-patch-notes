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
    public DbSet<Summary> Summaries => Set<Summary>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Package>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.NpmName).HasMaxLength(256);
            entity.Property(e => e.GithubOwner).HasMaxLength(128);
            entity.Property(e => e.GithubRepo).HasMaxLength(128);
            entity.HasIndex(e => e.NpmName).IsUnique();
        });

        modelBuilder.Entity<Release>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.Property(e => e.Version).HasMaxLength(128);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => new { e.PackageId, e.Version }).IsUnique();
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Releases)
                .HasForeignKey(e => e.PackageId);
        });

        modelBuilder.Entity<Summary>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.Property(e => e.VersionGroup).HasMaxLength(64);
            entity.HasIndex(e => new { e.PackageId, e.VersionGroup, e.Period, e.PeriodStart }).IsUnique();
            entity.HasOne(e => e.Package)
                .WithMany()
                .HasForeignKey(e => e.PackageId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.StytchUserId).HasMaxLength(128);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(64);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(64);
            entity.Property(e => e.SubscriptionStatus).HasMaxLength(32);
            entity.HasIndex(e => e.StytchUserId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.StripeCustomerId);
        });

        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.UserId).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.HasIndex(e => new { e.UserId, e.PackageId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.Watchlists)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Watchlists)
                .HasForeignKey(e => e.PackageId);
        });
    }
}
