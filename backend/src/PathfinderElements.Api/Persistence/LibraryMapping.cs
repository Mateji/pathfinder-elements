using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PathfinderElements.Api.Library;

namespace PathfinderElements.Api.Persistence;

internal static class LibraryMapping
{
    private static readonly JsonSerializerOptions AttackStorageJson = new(JsonSerializerDefaults.Web);

    internal static void Configure(ModelBuilder modelBuilder)
    {
        var characters = modelBuilder.Entity<CharacterTemplate>();
        characters.ToTable("character_templates", table =>
        {
            table.HasCheckConstraint("ck_character_templates_max_hit_points", "max_hit_points > 0");
        });
        characters.HasKey(template => template.Id);
        characters.Property(template => template.Id).HasColumnName("id").ValueGeneratedNever();
        characters.Property(template => template.OwnerUserId).HasColumnName("owner_user_id").IsRequired().IsConcurrencyToken();
        characters.Property(template => template.Name).HasColumnName("name").IsRequired();
        characters.Property(template => template.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        characters.Property(template => template.Version).HasColumnName("version").HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        characters.HasOne<IdentityUser>().WithMany()
            .HasForeignKey(template => template.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        characters.ComplexProperty(template => template.Statistics, ConfigureStatistics);

        var monsters = modelBuilder.Entity<MonsterTemplate>();
        monsters.ToTable("monster_templates", table =>
        {
            table.HasCheckConstraint("ck_monster_templates_max_hit_points", "max_hit_points > 0");
        });
        monsters.HasKey(template => template.Id);
        monsters.Property(template => template.Id).HasColumnName("id").ValueGeneratedNever();
        monsters.Property(template => template.OwnerUserId).HasColumnName("owner_user_id").IsRequired().IsConcurrencyToken();
        monsters.Property(template => template.Name).HasColumnName("name").IsRequired();
        monsters.Property(template => template.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        monsters.Property(template => template.Version).HasColumnName("version").HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        monsters.HasOne<IdentityUser>().WithMany()
            .HasForeignKey(template => template.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        monsters.ComplexProperty(template => template.Statistics, ConfigureStatistics);
        var attacks = monsters.Property(template => template.AttackSequences)
            .HasColumnName("attack_sequences").HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                sequences => JsonSerializer.Serialize(sequences, AttackStorageJson),
                json => JsonSerializer.Deserialize<ImmutableArray<AttackSequence>>(json, AttackStorageJson));
        // The sequence, attack and damage records contain only immutable values and collections.
        attacks.Metadata.SetValueComparer(new ValueComparer<ImmutableArray<AttackSequence>>(
            (left, right) => left.Equals(right), sequences => sequences.GetHashCode(), sequences => sequences));
    }

    private static void ConfigureStatistics(ComplexPropertyBuilder<CombatStatistics> statistics)
    {
        statistics.IsRequired();
        statistics.Property(values => values.MaxHitPoints).HasColumnName("max_hit_points");
        statistics.Property(values => values.InitiativeModifier).HasColumnName("initiative_modifier");
        statistics.Property(values => values.ArmorClass).HasColumnName("armor_class");
        statistics.Property(values => values.TouchArmorClass).HasColumnName("touch_armor_class");
        statistics.Property(values => values.FlatFootedArmorClass).HasColumnName("flat_footed_armor_class");
        statistics.Property(values => values.FortitudeSave).HasColumnName("fortitude_save");
        statistics.Property(values => values.ReflexSave).HasColumnName("reflex_save");
        statistics.Property(values => values.WillSave).HasColumnName("will_save");
    }
}
