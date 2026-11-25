using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PathfinderElements.Api.Library;
using PathfinderElements.Api.Persistence;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public sealed class LibraryApiTests
{
    private const string TestPassword = "Synthetic-Library-Test123!";
    private static readonly CombatStatistics Statistics = new(24, -1, 17, 9, 18, 4, -2, 0);

    [Theory]
    [InlineData("characters")]
    [InlineData("monsters")]
    public async Task AllRoutesRequireAnAuthenticatedSession(string collection)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        var list = $"/api/{collection}";
        var detail = $"{list}/{Guid.NewGuid()}";
        var requests = new (HttpMethod Method, string Path, object? Body)[]
        {
            (HttpMethod.Get, list, null),
            (HttpMethod.Get, detail, null),
            (HttpMethod.Post, list, new { name = "Private template", Statistics }),
            (HttpMethod.Put, detail, new { name = "Private template", Statistics }),
            (HttpMethod.Put, $"{detail}/archive", new { isArchived = true }),
            (HttpMethod.Delete, detail, null)
        };
        foreach (var (method, path, body) in requests)
        {
            using var response = await SendAsync(client, method, path, body);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [PostgreSqlTheory]
    [InlineData("characters")]
    [InlineData("monsters")]
    public async Task IsolatesOwnersAndRejectsRequestsWithInvalidInputOrCsrf(string collection)
    {
        await using var factory = new LibraryApiFactory();
        var (owner, otherOwner) = await SeedUsersAsync(factory);
        using var client = CreateClient(factory);
        using var otherClient = CreateClient(factory);
        var csrf = await SignInAsync(client, owner.UserName!);
        var otherCsrf = await SignInAsync(otherClient, otherOwner.UserName!);
        var list = $"/api/{collection}";
        var write = new { name = "Owned template", Statistics };

        using var missingCsrf = await SendAsync(client, HttpMethod.Post, list, write);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var wrongCsrf = await SendAsync(client, HttpMethod.Post, list, write, otherCsrf);
        Assert.Equal(HttpStatusCode.BadRequest, wrongCsrf.StatusCode);
        using var injectedOwner = await SendAsync(client, HttpMethod.Post, list,
            new { write.name, Statistics, ownerUserId = otherOwner.Id }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, injectedOwner.StatusCode);
        using var missingStatisticsField = await SendAsync(client, HttpMethod.Post, list,
            new { write.name, statistics = new { maxHitPoints = 24 } }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, missingStatisticsField.StatusCode);
        using var invalidHitPoints = await SendAsync(client, HttpMethod.Post, list,
            new { write.name, statistics = WithInvalidHitPoints() }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, invalidHitPoints.StatusCode);
        using var blankName = await SendAsync(client, HttpMethod.Post, list, new { name = " ", Statistics }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        using var invalidName = await SendAsync(client, HttpMethod.Post, list, new { name = "Null\0name", Statistics }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, invalidName.StatusCode);

        var (itemPath, saved, etag) = await CreateTemplateAsync(client, list, write.name, csrf);
        var (_, otherTemplate, _) = await CreateTemplateAsync(otherClient, list, "Foreign template", otherCsrf);
        Assert.Equal(Statistics, saved.Statistics);
        Assert.False(saved.IsArchived);
        var owned = await client.GetFromJsonAsync<Template[]>(list);
        Assert.Equal(saved, Assert.Single(owned!));
        var otherOwned = await otherClient.GetFromJsonAsync<Template[]>($"{list}?includeArchived=true");
        Assert.Equal(otherTemplate, Assert.Single(otherOwned!));

        using var privateRead = await client.GetAsync(itemPath);
        Assert.Equal(HttpStatusCode.OK, privateRead.StatusCode);
        Assert.True(privateRead.Headers.CacheControl?.NoStore);
        using var body = JsonDocument.Parse(await privateRead.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("ownerUserId", out _));

        using var missingUpdateCsrf = await SendAsync(client, HttpMethod.Put, itemPath, write, etag: etag);
        Assert.Equal(HttpStatusCode.BadRequest, missingUpdateCsrf.StatusCode);
        using var missingArchiveCsrf = await SendAsync(client, HttpMethod.Put, $"{itemPath}/archive",
            new { isArchived = true }, etag: etag);
        Assert.Equal(HttpStatusCode.BadRequest, missingArchiveCsrf.StatusCode);
        using var missingDeleteCsrf = await SendAsync(client, HttpMethod.Delete, itemPath, etag: etag);
        Assert.Equal(HttpStatusCode.BadRequest, missingDeleteCsrf.StatusCode);

        using var foreignGet = await otherClient.GetAsync(itemPath);
        using var foreignPut = await SendAsync(otherClient, HttpMethod.Put, itemPath, write, otherCsrf, etag);
        using var foreignArchive = await SendAsync(otherClient, HttpMethod.Put, $"{itemPath}/archive",
            new { isArchived = true }, otherCsrf, etag);
        using var foreignDelete = await SendAsync(otherClient, HttpMethod.Delete, itemPath, csrf: otherCsrf, etag: etag);
        Assert.All(new[] { foreignGet, foreignPut, foreignArchive, foreignDelete },
            response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        Assert.Equal(saved, await client.GetFromJsonAsync<Template>(itemPath));

        static object WithInvalidHitPoints()
        {
            return new
            {
                MaxHitPoints = 0,
                Statistics.InitiativeModifier,
                Statistics.ArmorClass,
                Statistics.TouchArmorClass,
                Statistics.FlatFootedArmorClass,
                Statistics.FortitudeSave,
                Statistics.ReflexSave,
                Statistics.WillSave
            };
        }
    }

    [PostgreSqlTheory]
    [InlineData("characters")]
    [InlineData("monsters")]
    public async Task UpdatesWithTheCurrentVersionAndArchivesWithoutBreakingReferences(string collection)
    {
        await using var factory = new LibraryApiFactory();
        var (owner, _) = await SeedUsersAsync(factory);
        using var client = CreateClient(factory);
        var csrf = await SignInAsync(client, owner.UserName!);
        var list = $"/api/{collection}";
        var (itemPath, created, initialTag) = await CreateTemplateAsync(client, list, "Before update", csrf);
        var changedStatistics = Statistics with { InitiativeModifier = 5, ReflexSave = 0 };
        var update = new { name = "After update", statistics = changedStatistics };

        using var withoutTag = await SendAsync(client, HttpMethod.Put, itemPath, update, csrf);
        Assert.Equal((HttpStatusCode)428, withoutTag.StatusCode);
        using var updated = await SendAsync(client, HttpMethod.Put, itemPath, update, csrf, initialTag);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(new Template(created.Id, update.name, changedStatistics, false),
            await updated.Content.ReadFromJsonAsync<Template>());
        var updatedTag = GetTag(updated);
        Assert.NotEqual(initialTag, updatedTag);

        using var staleUpdate = await SendAsync(client, HttpMethod.Put, itemPath, new { name = "Stale", Statistics }, csrf, initialTag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleUpdate.StatusCode);
        using var wildcard = await SendAsync(client, HttpMethod.Delete, itemPath, csrf: csrf, etag: "*");
        Assert.Equal(HttpStatusCode.PreconditionFailed, wildcard.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<PathfinderDbContext>();
        var createReferenceTable = collection switch
        {
            "characters" => "CREATE TABLE prepared_reference_probe (template_id uuid REFERENCES character_templates(id) ON DELETE RESTRICT)",
            "monsters" => "CREATE TABLE prepared_reference_probe (template_id uuid REFERENCES monster_templates(id) ON DELETE RESTRICT)",
            _ => throw new ArgumentOutOfRangeException(nameof(collection))
        };
        await database.Database.ExecuteSqlRawAsync(createReferenceTable);
        try
        {
            await database.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO prepared_reference_probe (template_id) VALUES ({created.Id})");
            using var archived = await SendAsync(client, HttpMethod.Put, $"{itemPath}/archive", new { isArchived = true }, csrf, updatedTag);
            Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
            var archivedTag = GetTag(archived);
            Assert.NotEqual(updatedTag, archivedTag);

            Assert.Empty((await client.GetFromJsonAsync<Template[]>(list))!);
            var archivedTemplate = new Template(created.Id, update.name, changedStatistics, true);
            Assert.Equal(archivedTemplate, await client.GetFromJsonAsync<Template>(itemPath));
            Assert.Equal(archivedTemplate, Assert.Single((await client.GetFromJsonAsync<Template[]>($"{list}?includeArchived=true"))!));

            using var blockedDelete = await SendAsync(client, HttpMethod.Delete, itemPath, csrf: csrf, etag: archivedTag);
            Assert.Equal(HttpStatusCode.Conflict, blockedDelete.StatusCode);
            Assert.Equal(archivedTemplate, await client.GetFromJsonAsync<Template>(itemPath));

            using var restored = await SendAsync(client, HttpMethod.Put, $"{itemPath}/archive", new { isArchived = false }, csrf, archivedTag);
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
            var restoredTag = GetTag(restored);
            Assert.NotEqual(archivedTag, restoredTag);
            Assert.False(Assert.Single((await client.GetFromJsonAsync<Template[]>(list))!).IsArchived);

            await database.Database.ExecuteSqlRawAsync("DELETE FROM prepared_reference_probe");
            using var deleted = await SendAsync(client, HttpMethod.Delete, itemPath, csrf: csrf, etag: restoredTag);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            using var afterDelete = await client.GetAsync(itemPath);
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        }
        finally
        {
            await database.Database.ExecuteSqlRawAsync("DROP TABLE prepared_reference_probe");
        }
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task<(IdentityUser Owner, IdentityUser Other)> SeedUsersAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PathfinderDbContext>().Database.MigrateAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var owner = new IdentityUser($"owner-{Guid.NewGuid():N}");
        var other = new IdentityUser($"other-{Guid.NewGuid():N}");
        Assert.True((await users.CreateAsync(owner, TestPassword)).Succeeded);
        Assert.True((await users.CreateAsync(other, TestPassword)).Succeeded);
        return (owner, other);
    }

    private static async Task<string> SignInAsync(HttpClient client, string username)
    {
        var initialCsrf = await client.GetFromJsonAsync<CsrfToken>("/api/auth/csrf");
        using var login = await SendAsync(client, HttpMethod.Post, "/api/auth/login", new { username, password = TestPassword },
            Assert.IsType<CsrfToken>(initialCsrf).Token);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfToken>("/api/auth/csrf");
        return Assert.IsType<CsrfToken>(csrf).Token;
    }

    private static async Task<(string Path, Template Template, string Tag)> CreateTemplateAsync(
        HttpClient client, string list, string name, string csrf)
    {
        using var response = await SendAsync(client, HttpMethod.Post, list, new { name, Statistics }, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var template = Assert.IsType<Template>(await response.Content.ReadFromJsonAsync<Template>());
        var path = $"{list}/{template.Id}";
        Assert.EndsWith(path, response.Headers.Location?.ToString());
        return (path, template, GetTag(response));
    }

    private static string GetTag(HttpResponseMessage response)
    {
        var tag = response.Headers.ETag;
        Assert.NotNull(tag);
        Assert.False(tag.IsWeak);
        return tag.Tag;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, object? body = null, string? csrf = null, string? etag = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (csrf is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", csrf);
        }

        if (etag is not null)
        {
            request.Headers.Add("If-Match", etag);
        }

        return await client.SendAsync(request);
    }

    private sealed record Template(Guid Id, string Name, CombatStatistics Statistics, bool IsArchived);
    private sealed record CsrfToken(string Token);

    private sealed class LibraryApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<PathfinderDbContext>>();
                services.AddDbContext<PathfinderDbContext>(options =>
                {
                    options.UseNpgsql(PostgreSqlTestDatabase.ConnectionString("pathfinder_api_tests"));
                });
            });
        }
    }
}
