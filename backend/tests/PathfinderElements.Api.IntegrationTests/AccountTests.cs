using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PathfinderElements.Api.Persistence;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public sealed class AccountTests
{
    private const string Username = "test-account";
    private const string Password = "Sample-Password123!";

    [Fact]
    public async Task SessionRequiresLoginAndLoginAndLogoutRequireCsrf()
    {
        await using var factory = new AccountFactory();
        using var client = CreateClient(factory);
        await SeedAccountAsync(factory);

        using var sessionWithoutCookie = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, sessionWithoutCookie.StatusCode);

        using var missingCsrf = await client.PostAsJsonAsync("/api/auth/login", new { Username, Password });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using var invalidLogin = await LoginAsync(client, "Incorrect-Password123!");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);

        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-PathfinderSession="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);

        using var session = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        var identity = await session.Content.ReadFromJsonAsync<Session>();
        Assert.Equal(Username, identity?.Username);
        Assert.Equal("synthetic-owner", identity?.UserId);

        using var missingLogoutCsrf = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, missingLogoutCsrf.StatusCode);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("X-CSRF-TOKEN", await GetCsrfAsync(client));
        using var loggedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
        using var afterLogout = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task ActivityDoesNotExtendSessionBeyondTwelveHours()
    {
        await using var factory = new AccountFactory();
        using var client = CreateClient(factory);
        await SeedAccountAsync(factory);

        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        factory.Clock.Advance(TimeSpan.FromHours(11));
        using var activeSession = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, activeSession.StatusCode);

        factory.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));
        using var expiredSession = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, expiredSession.StatusCode);
    }

    [Fact]
    public async Task PasswordResetInvalidatesOldSessions()
    {
        await using var factory = new AccountFactory();
        using var client = CreateClient(factory);
        await SeedAccountAsync(factory);

        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByNameAsync(Username) ?? throw new InvalidOperationException("Missing test user.");
        var token = await users.GeneratePasswordResetTokenAsync(user);
        Assert.True((await users.ResetPasswordAsync(user, token, "New-Sample-Password456!")).Succeeded);

        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        using var oldSession = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
    }

    [Fact]
    public async Task RejectsPlainHttpAndDoesNotExposeRegistration()
    {
        await using var factory = new AccountFactory();
        using var http = factory.CreateClient();
        using var insecure = await http.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.BadRequest, insecure.StatusCode);

        using var https = CreateClient(factory);
        await SeedAccountAsync(factory);
        using var login = await LoginAsync(https);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        using var registration = await https.PostAsJsonAsync("/api/auth/register", new { Username, Password });
        Assert.Equal(HttpStatusCode.NotFound, registration.StatusCode);
    }

    private static HttpClient CreateClient(AccountFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task SeedAccountAsync(AccountFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var result = await users.CreateAsync(new IdentityUser { Id = "synthetic-owner", UserName = Username }, Password);
        Assert.True(result.Succeeded);
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<Csrf>("/api/auth/csrf");
        return result?.Token ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string password = Password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfAsync(client));
        request.Content = JsonContent.Create(new { Username, Password = password });
        return await client.SendAsync(request);
    }

    private sealed record Csrf(string Token);
    private sealed record Session(string UserId, string Username);

    private sealed class AccountFactory : WebApplicationFactory<Program>
    {
        internal TestClock Clock { get; } = new();
        private readonly string databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<PathfinderDbContext>>();
                services.AddDbContext<PathfinderDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options => options.TimeProvider = Clock);
                services.Configure<SecurityStampValidatorOptions>(options => options.TimeProvider = Clock);
            });
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => now;

        internal void Advance(TimeSpan duration)
        {
            now += duration;
        }
    }
}
