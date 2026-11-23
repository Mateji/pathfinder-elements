var builder = WebApplication.CreateBuilder(args);
var application = builder.Build();

application.MapGet("/health/live", () =>
{
    return Results.Ok();
});

application.Run();

public partial class Program
{
}
