using Microsoft.EntityFrameworkCore;
using Npgsql;
using PathfinderElements.Api.Library;
using PathfinderElements.Api.Persistence;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public sealed class LibraryTemplateTests
{
    private const string PostgreSqlHostVariable = "PATHFINDER_TEST_POSTGRES_HOST";

    [PostgreSqlFact]
    public async Task RoundTripsLibraryTemplatesInPostgreSql()
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable(PostgreSqlHostVariable)
                ?? throw new InvalidOperationException("Missing PostgreSQL test host."),
            Database = "pathfinder",
            Username = "pathfinder",
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? throw new InvalidOperationException("Missing PostgreSQL test password.")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<PathfinderDbContext>()
            .UseNpgsql(connectionString).Options;
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

    private sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlHostVariable)))
            {
                Skip = "Runs against isolated PostgreSQL via backend/scripts/test-persistence.sh.";
            }
        }
    }
}
