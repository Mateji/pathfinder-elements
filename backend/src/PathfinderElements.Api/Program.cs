using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PathfinderElements.Api.Access;
using PathfinderElements.Api.Persistence;

var accountCommand = args.FirstOrDefault() == "account";
var builder = WebApplication.CreateBuilder(accountCommand ? [] : args);
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
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<PathfinderDbContext>().AddSignInManager().AddDefaultTokenProviders();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-PathfinderSession";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-PathfinderCsrf";
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-login", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("auth-csrf", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});
var protection = builder.Services.AddDataProtection().SetApplicationName("PathfinderElements");
if (builder.Configuration["Auth:KeyDirectory"] is { Length: > 0 } keyDirectory)
{
    protection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

if (builder.Configuration["Auth:TrustedProxy"] is { Length: > 0 } trustedProxy)
{
    var trustedAddress = IPAddress.Parse(trustedProxy);
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(trustedAddress);
    });
}

var application = builder.Build();

application.UseForwardedHeaders();
application.UseAuthentication();
application.UseAuthorization();
application.UseRateLimiter();
application.MapAccountEndpoints();

application.MapGet("/health/live", () =>
{
    return Results.Ok();
}).AllowAnonymous();

application.MapGet("/health/ready", async (PathfinderDbContext database, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(connectionString) || !await database.Database.CanConnectAsync(cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok();
}).AllowAnonymous();

if (!accountCommand && builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = application.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PathfinderDbContext>();
    await database.Database.MigrateAsync();
}

if (accountCommand)
{
    await using var scope = application.Services.CreateAsyncScope();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    return await AccountCommands.ExecuteAsync(args, users, Console.In, Console.Error);
}

await application.RunAsync();
return 0;

public partial class Program
{
}
