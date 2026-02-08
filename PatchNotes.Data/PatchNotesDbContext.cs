using Microsoft.EntityFrameworkCore;

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
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<ReleaseSummary> ReleaseSummaries => Set<ReleaseSummary>();

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
            entity.Property(e => e.Tag).HasMaxLength(128);
            entity.Property(e => e.SummaryStale).HasDefaultValue(true);
            entity.Property(e => e.SummaryVersion).IsConcurrencyToken();
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => new { e.PackageId, e.Tag }).IsUnique();
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Releases)
                .HasForeignKey(e => e.PackageId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(21);
            entity.Property(e => e.PackageId).HasMaxLength(21);
            entity.Property(e => e.GitHubId).HasMaxLength(64);
            entity.Property(e => e.Reason).HasMaxLength(64);
            entity.Property(e => e.SubjectType).HasMaxLength(64);
            entity.Property(e => e.RepositoryFullName).HasMaxLength(256);
            entity.HasIndex(e => e.GitHubId).IsUnique();
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.Unread);
            entity.HasOne(e => e.Package)
                .WithMany()
                .HasForeignKey(e => e.PackageId)
                .IsRequired(false);
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
