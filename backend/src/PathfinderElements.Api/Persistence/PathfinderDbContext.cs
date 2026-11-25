using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PathfinderElements.Api.Encounters;
using PathfinderElements.Api.Library;

namespace PathfinderElements.Api.Persistence;

public sealed class PathfinderDbContext(DbContextOptions<PathfinderDbContext> options) : IdentityUserContext<IdentityUser>(options)
{
    public DbSet<ApplicationMetadata> ApplicationMetadata => Set<ApplicationMetadata>();
    public DbSet<CharacterTemplate> CharacterTemplates => Set<CharacterTemplate>();
    public DbSet<MonsterTemplate> MonsterTemplates => Set<MonsterTemplate>();
    internal DbSet<PreparedEncounter> PreparedEncounters => Set<PreparedEncounter>();
    internal DbSet<EncounterParticipant> EncounterParticipants => Set<EncounterParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var metadata = modelBuilder.Entity<ApplicationMetadata>();
        metadata.ToTable("application_metadata");
        metadata.HasKey(entry => entry.Key);
        metadata.Property(entry => entry.Key).HasColumnName("key").HasMaxLength(128);
        metadata.Property(entry => entry.Value).HasColumnName("value");

        LibraryMapping.Configure(modelBuilder);
        EncounterMapping.Configure(modelBuilder);
    }
}

public sealed class ApplicationMetadata
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}
