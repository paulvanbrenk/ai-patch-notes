using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PatchNotes.Data;

public class PatchNotesDbContext : DbContext
{
    public PatchNotesDbContext(DbContextOptions<PatchNotesDbContext> options)
        : base(options)
    {
    }

    protected PatchNotesDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IHasCreatedAt>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
        }

        foreach (var entry in ChangeTracker.Entries<IHasUpdatedAt>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Release> Releases => Set<Release>();
public DbSet<User> Users => Set<User>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<ReleaseSummary> ReleaseSummaries => Set<ReleaseSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite cannot translate DateTimeOffset comparisons. Store as DateTime (UTC)
        // so queries like .Where(r => r.PublishedAt >= cutoff) work on both providers.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var dtoConverter = new ValueConverter<DateTimeOffset, DateTime>(
                v => v.UtcDateTime,
                v => new DateTimeOffset(v, TimeSpan.Zero));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(dtoConverter);
                    }
                }
            }
        }

        modelBuilder.Entity<Package>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.NpmName).HasMaxLength(256);
            entity.Property(e => e.GithubOwner).HasMaxLength(128);
            entity.Property(e => e.GithubRepo).HasMaxLength(128);
            entity.Property(e => e.TagPrefix).HasMaxLength(64);
            entity.HasIndex(e => e.NpmName).IsUnique();
        });

        modelBuilder.Entity<Release>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.Property(e => e.Tag).HasMaxLength(128);
            entity.Property(e => e.SummaryStale).HasDefaultValue(true);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => new { e.PackageId, e.Tag }).IsUnique();
            entity.HasIndex(e => new { e.PackageId, e.MajorVersion, e.IsPrerelease });
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Releases)
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

        modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(128);
        });

        modelBuilder.Entity<ReleaseSummary>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.HasIndex(e => new { e.PackageId, e.MajorVersion, e.IsPrerelease }).IsUnique();
            entity.HasOne(e => e.Package)
                .WithMany(p => p.ReleaseSummaries)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
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
