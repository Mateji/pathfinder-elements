using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PathfinderElements.Api.Library;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public sealed partial class LibraryApiTests
{
    private const string EncountersPath = "/api/encounters";

    [Fact]
    public async Task AllEncounterRoutesRequireAnAuthenticatedSession()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        var detail = $"{EncountersPath}/{Guid.NewGuid()}";
        var requests = new (HttpMethod Method, string Path, object? Body)[]
        {
            (HttpMethod.Get, EncountersPath, null),
            (HttpMethod.Get, detail, null),
            (HttpMethod.Post, EncountersPath, new { name = "Private" }),
            (HttpMethod.Put, detail, new { name = "Private" }),
            (HttpMethod.Post, $"{detail}/participants", new { kind = "monsters", templateId = Guid.NewGuid() }),
            (HttpMethod.Delete, $"{detail}/participants/{Guid.NewGuid()}", null)
        };
        foreach (var (method, path, body) in requests)
        {
            using var response = await SendAsync(client, method, path, body);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [PostgreSqlFact]
    public async Task EncounterKeepsIndividualParticipantsAndRestrictsDeletingTheirTemplates()
    {
        await using var factory = new LibraryApiFactory();
        var (owner, _) = await SeedUsersAsync(factory);
        using var client = CreateClient(factory);
        var csrf = await SignInAsync(client, owner.UserName!);
        var (path, saved) = await CreateEncounterAsync(client, "  Am Tor  ", csrf);
        Assert.Equal("Am Tor", saved.Detail.Name);

        foreach (var name in new[] { "PC 1", "PC 2", "PC 3" })
        {
            var (_, template, _) = await CreateTemplateAsync(client, "/api/characters", name, csrf);
            saved = await AddAsync(client, path, new { kind = "characters", templateId = template.Id }, saved.Tag, csrf);
        }

        var (goblinPath, goblin, goblinTag) = await CreateTemplateAsync(client, "/api/monsters", "Goblin", csrf);
        var (orcPath, orc, orcTag) = await CreateTemplateAsync(client, "/api/monsters", "Ork", csrf);
        saved = await AddAsync(client, path, new { kind = "monsters", templateId = goblin.Id, quantity = 4 }, saved.Tag, csrf);
        saved = await AddAsync(client, path, new { kind = "monsters", templateId = orc.Id }, saved.Tag, csrf);
        var roster = saved.Detail.Participants;
        Assert.Equal(8, roster.Length);
        Assert.Equal(8, roster.Select(entry => entry.Id).Distinct().Count());
        Assert.Equal(3, roster.Count(entry => entry.Kind == "characters"));
        Assert.Equal(4, roster.Count(entry => entry.TemplateId == goblin.Id));
        var orcEntry = Assert.Single(roster, entry => entry.TemplateId == orc.Id);

        using var renamed = await SendAsync(client, HttpMethod.Put, path, new { name = "Am Nordtor" }, csrf, saved.Tag);
        saved = await ReadEncounterAsync(renamed);
        Assert.Equal("Am Nordtor", saved.Detail.Name);
        Assert.Equal(roster, saved.Detail.Participants);

        var updatedStatistics = new CombatStatistics(9, -1, 17, 9, 18, 4, -2, 0);
        using var updatedTemplate = await SendAsync(client, HttpMethod.Put, goblinPath,
            new { name = "Goblin-Späher", statistics = updatedStatistics }, csrf, goblinTag);
        updatedTemplate.EnsureSuccessStatusCode();
        using var archived = await SendAsync(client, HttpMethod.Put, $"{goblinPath}/archive",
            new { isArchived = true }, csrf, GetTag(updatedTemplate));
        archived.EnsureSuccessStatusCode();
        using var stale = await SendAsync(client, HttpMethod.Put, path, new { name = "Stale" }, csrf, saved.Tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using var reopened = await client.GetAsync(path);
        Assert.True(reopened.Headers.CacheControl?.NoStore);
        saved = await ReadEncounterAsync(reopened);
        Assert.Equal(roster.Select(entry => entry.Id), saved.Detail.Participants.Select(entry => entry.Id));
        Assert.All(saved.Detail.Participants.Where(entry => entry.TemplateId == goblin.Id), entry =>
        {
            Assert.True(entry.IsArchived);
            Assert.Equal("Goblin-Späher", entry.Name);
            Assert.Equal(9, entry.MaxHitPoints);
        });
        using var addArchived = await SendAsync(client, HttpMethod.Post, $"{path}/participants",
            new { kind = "monsters", templateId = goblin.Id }, csrf, saved.Tag);
        Assert.Equal(HttpStatusCode.NotFound, addArchived.StatusCode);

        using var deleteGoblin = await SendAsync(client, HttpMethod.Delete, goblinPath, csrf: csrf, etag: GetTag(archived));
        Assert.Equal(HttpStatusCode.Conflict, deleteGoblin.StatusCode);
        using var deleteOrc = await SendAsync(client, HttpMethod.Delete, orcPath, csrf: csrf, etag: orcTag);
        Assert.Equal(HttpStatusCode.Conflict, deleteOrc.StatusCode);
        using var remove = await SendAsync(client, HttpMethod.Delete, $"{path}/participants/{orcEntry.Id}", csrf: csrf, etag: saved.Tag);
        saved = await ReadEncounterAsync(remove);
        Assert.Equal(roster.Where(entry => entry.Id != orcEntry.Id).Select(entry => entry.Id), saved.Detail.Participants.Select(entry => entry.Id));
        using var deleteAfterRemoval = await SendAsync(client, HttpMethod.Delete, orcPath, csrf: csrf, etag: orcTag);
        Assert.Equal(HttpStatusCode.NoContent, deleteAfterRemoval.StatusCode);
        var summaries = await client.GetFromJsonAsync<EncounterSummary[]>(EncountersPath);
        Assert.Equal(new EncounterSummary(saved.Detail.Id, "Am Nordtor", 7), Assert.Single(summaries!));
    }

    [PostgreSqlFact]
    public async Task EncounterRejectsForeignReferencesAndOnlyCommitsOneConcurrentChange()
    {
        await using var factory = new LibraryApiFactory();
        var (owner, otherOwner) = await SeedUsersAsync(factory);
        using var client = CreateClient(factory);
        using var otherClient = CreateClient(factory);
        var csrf = await SignInAsync(client, owner.UserName!);
        var otherCsrf = await SignInAsync(otherClient, otherOwner.UserName!);
        var (_, monster, _) = await CreateTemplateAsync(client, "/api/monsters", "Owned", csrf);
        var (_, otherMonster, _) = await CreateTemplateAsync(otherClient, "/api/monsters", "Foreign", otherCsrf);

        using var noCsrf = await SendAsync(client, HttpMethod.Post, EncountersPath, new { name = "Missing CSRF" });
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var blankName = await SendAsync(client, HttpMethod.Post, EncountersPath, new { name = "  " }, csrf);
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        var (path, saved) = await CreateEncounterAsync(client, "Owned encounter", csrf);
        Assert.Empty((await otherClient.GetFromJsonAsync<EncounterSummary[]>(EncountersPath))!);
        using var foreignRead = await otherClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);
        using var foreignRename = await SendAsync(otherClient, HttpMethod.Put, path, new { name = "Foreign" }, otherCsrf, saved.Tag);
        Assert.Equal(HttpStatusCode.NotFound, foreignRename.StatusCode);

        var participants = $"{path}/participants";
        var selection = new { kind = "monsters", templateId = monster.Id, quantity = 4 };
        using var foreignAdd = await SendAsync(otherClient, HttpMethod.Post, participants, selection, otherCsrf, saved.Tag);
        Assert.Equal(HttpStatusCode.NotFound, foreignAdd.StatusCode);
        using var foreignTemplate = await SendAsync(client, HttpMethod.Post, participants,
            new { kind = "monsters", templateId = otherMonster.Id }, csrf, saved.Tag);
        Assert.Equal(HttpStatusCode.NotFound, foreignTemplate.StatusCode);
        using var noWriteCsrf = await SendAsync(client, HttpMethod.Post, participants, selection, etag: saved.Tag);
        Assert.Equal(HttpStatusCode.BadRequest, noWriteCsrf.StatusCode);
        using var noVersion = await SendAsync(client, HttpMethod.Post, participants, selection, csrf);
        Assert.Equal((HttpStatusCode)428, noVersion.StatusCode);
        foreach (var quantity in new[] { 0, 101 })
        {
            using var invalid = await SendAsync(client, HttpMethod.Post, participants, selection with { quantity = quantity }, csrf, saved.Tag);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            SendAsync(client, HttpMethod.Post, participants, selection, csrf, saved.Tag)));
        using var first = results[0];
        using var second = results[1];
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.PreconditionFailed);
        using var reopened = await client.GetAsync(path);
        saved = await ReadEncounterAsync(reopened);
        Assert.Equal(4, saved.Detail.Participants.Length);
    }

    private static async Task<(string Path, VersionedPreparedEncounter Saved)> CreateEncounterAsync(
        HttpClient client, string name, string csrf)
    {
        using var response = await SendAsync(client, HttpMethod.Post, EncountersPath, new { name }, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = await ReadEncounterAsync(response);
        var path = $"{EncountersPath}/{saved.Detail.Id}";
        Assert.EndsWith(path, response.Headers.Location?.ToString());
        return (path, saved);
    }

    private static async Task<VersionedPreparedEncounter> AddAsync(HttpClient client, string path, object selection,
        string tag, string csrf)
    {
        using var response = await SendAsync(client, HttpMethod.Post, $"{path}/participants", selection, csrf, tag);
        return await ReadEncounterAsync(response);
    }

    private static async Task<VersionedPreparedEncounter> ReadEncounterAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var detail = Assert.IsType<EncounterDetail>(await response.Content.ReadFromJsonAsync<EncounterDetail>());
        return new VersionedPreparedEncounter(detail, GetTag(response));
    }

    private sealed record EncounterSummary(Guid Id, string Name, int ParticipantCount);
    private sealed record EncounterDetail(Guid Id, string Name, EncounterParticipant[] Participants);
    private sealed record EncounterParticipant(Guid Id, string Kind, Guid TemplateId, string Name, int MaxHitPoints, bool IsArchived);
    private sealed record VersionedPreparedEncounter(EncounterDetail Detail, string Tag);
}
