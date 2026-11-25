using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PathfinderElements.Api.Library;
using PathfinderElements.Api.Persistence;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public sealed class LibraryTemplateTests
{
    [PostgreSqlFact]
    public async Task RoundTripsLibraryTemplatesInPostgreSql()
    {
        var options = new DbContextOptionsBuilder<PathfinderDbContext>()
            .UseNpgsql(PostgreSqlTestDatabase.ConnectionString()).Options;
        var characterStatistics = new CombatStatistics(24, -1, 17, 9, 18, 4, -2, 0);
        var monsterStatistics = new CombatStatistics(1, 3, 13, 12, 11, 0, 4, -1);

        await using var writer = new PathfinderDbContext(options);
        var owner = await writer.Users.SingleAsync(user => user.NormalizedUserName == "BACKUP-TEST");
        var character = new CharacterTemplate(owner.Id, "Testfigur", characterStatistics);
        var monster = new MonsterTemplate(owner.Id, "Testfigur", monsterStatistics);
        writer.CharacterTemplates.Add(character);
        writer.MonsterTemplates.Add(monster);
        await writer.SaveChangesAsync();

        await using var reader = new PathfinderDbContext(options);
        var storedCharacter = await reader.CharacterTemplates.SingleAsync();
        var storedMonster = await reader.MonsterTemplates.SingleAsync();
        Assert.NotEqual(Guid.Empty, storedCharacter.Id);
        Assert.NotEqual(Guid.Empty, storedMonster.Id);
        Assert.NotEqual(storedCharacter.Id, storedMonster.Id);
        Assert.Equal(character.Id, storedCharacter.Id);
        Assert.Equal(monster.Id, storedMonster.Id);
        Assert.Equal(owner.Id, storedCharacter.OwnerUserId);
        Assert.Equal(owner.Id, storedMonster.OwnerUserId);
        Assert.Equal(character.Name, storedCharacter.Name);
        Assert.Equal(monster.Name, storedMonster.Name);
        Assert.Equal(characterStatistics, storedCharacter.Statistics);
        Assert.Equal(monsterStatistics, storedMonster.Statistics);

        var missingOwnerUserId = Guid.NewGuid().ToString();
        var invalidOwner = await Assert.ThrowsAsync<PostgresException>(() => reader.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE character_templates SET owner_user_id = {missingOwnerUserId} WHERE id = {storedCharacter.Id}"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, invalidOwner.SqlState);

        var invalidHitPoints = await Assert.ThrowsAsync<PostgresException>(() => reader.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE monster_templates SET max_hit_points = 0 WHERE id = {storedMonster.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, invalidHitPoints.SqlState);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveMaximumHitPoints(int maxHitPoints)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new CombatStatistics(maxHitPoints, 0, 10, 10, 10, 0, 0, 0));

        Assert.Equal("maxHitPoints", exception.ParamName);
    }

    [Theory]
    [InlineData(" ", "Testfigur", "ownerUserId")]
    [InlineData("test-owner", " ", "name")]
    public void BothTemplateTypesRequireAnOwnerAndName(string ownerUserId, string name, string parameterName)
    {
        var statistics = new CombatStatistics(1, 0, 10, 10, 10, 0, 0, 0);

        Assert.Equal(parameterName, Assert.Throws<ArgumentException>(() => new CharacterTemplate(ownerUserId, name, statistics)).ParamName);
        Assert.Equal(parameterName, Assert.Throws<ArgumentException>(() => new MonsterTemplate(ownerUserId, name, statistics)).ParamName);
    }

    [Fact]
    public void PostgreSqlModelMatchesTheLatestMigration()
    {
        var options = new DbContextOptionsBuilder<PathfinderDbContext>().UseNpgsql().Options;
        using var database = new PathfinderDbContext(options);

        Assert.False(database.Database.HasPendingModelChanges());
    }

    [PostgreSqlFact]
    public async Task ExistingTemplatesRemainActiveAndVersionChecksReachPostgreSql()
    {
        var options = new DbContextOptionsBuilder<PathfinderDbContext>()
            .UseNpgsql(PostgreSqlTestDatabase.ConnectionString("pathfinder_upgrade_tests")).Options;
        var owner = new IdentityUser("upgrade-test");
        var characterId = Guid.NewGuid();
        var monsterId = Guid.NewGuid();
        var statistics = new CombatStatistics(24, -1, 17, 9, 18, 4, -2, 0);
        await using var database = new PathfinderDbContext(options);
        await database.GetService<IMigrator>().MigrateAsync("AddLibraryTemplates");
        database.Users.Add(owner);
        await database.SaveChangesAsync();
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO character_templates (id, owner_user_id, name, max_hit_points, initiative_modifier, armor_class,
                touch_armor_class, flat_footed_armor_class, fortitude_save, reflex_save, will_save)
            VALUES ({characterId}, {owner.Id}, 'Vorhandener PC', 24, -1, 17, 9, 18, 4, -2, 0);
            INSERT INTO monster_templates (id, owner_user_id, name, max_hit_points, initiative_modifier, armor_class,
                touch_armor_class, flat_footed_armor_class, fortitude_save, reflex_save, will_save)
            VALUES ({monsterId}, {owner.Id}, 'Vorhandenes Monster', 24, -1, 17, 9, 18, 4, -2, 0);
            """);
        await database.Database.MigrateAsync();

        var character = await database.CharacterTemplates.SingleAsync();
        var monster = await database.MonsterTemplates.SingleAsync();
        Assert.Equal((characterId, owner.Id, "Vorhandener PC", statistics, false),
            (character.Id, character.OwnerUserId, character.Name, character.Statistics, character.IsArchived));
        Assert.Equal((monsterId, owner.Id, "Vorhandenes Monster", statistics, false),
            (monster.Id, monster.OwnerUserId, monster.Name, monster.Statistics, monster.IsArchived));
        Assert.NotEqual(Guid.Empty, character.Version);
        Assert.NotEqual(Guid.Empty, monster.Version);

        await using var concurrentDatabase = new PathfinderDbContext(options);
        var staleCharacter = await concurrentDatabase.CharacterTemplates.SingleAsync();
        Assert.Equal(character.Version, staleCharacter.Version);
        character.Update("Erste Änderung", statistics);
        staleCharacter.SetArchived(true);
        await database.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => concurrentDatabase.SaveChangesAsync());
    }
}
