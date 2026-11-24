using Microsoft.EntityFrameworkCore;
using Npgsql;
using PathfinderElements.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Pathfinder");
if (connectionString is null && builder.Configuration["Database:Host"] is string databaseHost)
{
    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = databaseHost,
        Database = builder.Configuration["Database:Name"],
        Username = builder.Configuration["Database:Username"],
        Password = builder.Configuration["Database:Password"],
        Timeout = 3
    }.ConnectionString;
}

builder.Services.AddDbContext<PathfinderDbContext>(options => options.UseNpgsql(connectionString));
var application = builder.Build();

application.MapGet("/health/live", () =>
{
    return Results.Ok();
});

application.MapGet("/health/ready", async (PathfinderDbContext database, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(connectionString) || !await database.Database.CanConnectAsync(cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok();
});

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = application.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PathfinderDbContext>();
    await database.Database.MigrateAsync();
}

application.Run();

public partial class Program
{
}
