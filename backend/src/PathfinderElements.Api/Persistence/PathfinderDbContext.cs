using Microsoft.EntityFrameworkCore;

namespace PathfinderElements.Api.Persistence;

public sealed class PathfinderDbContext(DbContextOptions<PathfinderDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationMetadata> ApplicationMetadata => Set<ApplicationMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var metadata = modelBuilder.Entity<ApplicationMetadata>();
        metadata.ToTable("application_metadata");
        metadata.HasKey(entry => entry.Key);
        metadata.Property(entry => entry.Key).HasColumnName("key").HasMaxLength(128);
        metadata.Property(entry => entry.Value).HasColumnName("value");
    }
}

public sealed class ApplicationMetadata
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}
