using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PathfinderElements.Api.Encounters;

namespace PathfinderElements.Api.Persistence;

internal static class EncounterMapping
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        var encounters = modelBuilder.Entity<PreparedEncounter>();
        encounters.ToTable("prepared_encounters");
        encounters.HasKey(encounter => encounter.Id);
        encounters.Property(encounter => encounter.Id).HasColumnName("id").ValueGeneratedNever();
        encounters.Property(encounter => encounter.OwnerUserId).HasColumnName("owner_user_id").IsConcurrencyToken();
        encounters.Property(encounter => encounter.Name).HasColumnName("name").HasMaxLength(256);
        encounters.Property(encounter => encounter.Version).HasColumnName("version").IsConcurrencyToken();
        encounters.HasOne<IdentityUser>().WithMany()
            .HasForeignKey(encounter => encounter.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        encounters.HasMany(encounter => encounter.Participants).WithOne()
            .HasForeignKey(participant => participant.EncounterId).OnDelete(DeleteBehavior.Cascade);

        var participants = modelBuilder.Entity<EncounterParticipant>();
        participants.ToTable("encounter_participants", table => table.HasCheckConstraint(
            "ck_encounter_participants_one_template", "(character_template_id IS NULL) <> (monster_template_id IS NULL)"));
        participants.HasKey(participant => participant.Id);
        participants.Property(participant => participant.Id).HasColumnName("id").ValueGeneratedNever();
        participants.Property(participant => participant.EncounterId).HasColumnName("encounter_id");
        participants.Property(participant => participant.CharacterTemplateId).HasColumnName("character_template_id");
        participants.Property(participant => participant.MonsterTemplateId).HasColumnName("monster_template_id");
        participants.HasOne(participant => participant.CharacterTemplate).WithMany()
            .HasForeignKey(participant => participant.CharacterTemplateId).OnDelete(DeleteBehavior.Restrict);
        participants.HasOne(participant => participant.MonsterTemplate).WithMany()
            .HasForeignKey(participant => participant.MonsterTemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}
