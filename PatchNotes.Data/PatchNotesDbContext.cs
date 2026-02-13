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

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Release> Releases => Set<Release>();
public DbSet<User> Users => Set<User>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<ReleaseSummary> ReleaseSummaries => Set<ReleaseSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQL Server returns DateTime without DateTimeKind set, causing
        // System.Text.Json to omit the 'Z' suffix. Force UTC kind so
        // serialized values are valid ISO 8601 (required by Zod).
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcConverter);
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
            entity.Property(e => e.SummaryVersion).HasMaxLength(21).IsConcurrencyToken();
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
