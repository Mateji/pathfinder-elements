using Npgsql;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

internal static class PostgreSqlTestDatabase
{
    private const string HostVariable = "PATHFINDER_TEST_POSTGRES_HOST";

    public static bool IsConfigured
    {
        get { return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(HostVariable)); }
    }

    public static string ConnectionString(string database = "pathfinder")
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable(HostVariable)
                ?? throw new InvalidOperationException("Missing PostgreSQL test host."),
            Database = database,
            Username = "pathfinder",
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? throw new InvalidOperationException("Missing PostgreSQL test password.")
        }.ConnectionString;
    }
}

internal sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!PostgreSqlTestDatabase.IsConfigured)
        {
            Skip = "Runs against isolated PostgreSQL via backend/scripts/test-persistence.sh.";
        }
    }
}

internal sealed class PostgreSqlTheoryAttribute : TheoryAttribute
{
    public PostgreSqlTheoryAttribute()
    {
        if (!PostgreSqlTestDatabase.IsConfigured)
        {
            Skip = "Runs against isolated PostgreSQL via backend/scripts/test-persistence.sh.";
        }
    }
}
