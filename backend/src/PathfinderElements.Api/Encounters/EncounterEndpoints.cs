using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using PathfinderElements.Api.Access;
using PathfinderElements.Api.Library;
using PathfinderElements.Api.Persistence;

namespace PathfinderElements.Api.Encounters;

internal static class EncounterEndpoints
{
    private const string DetailRoute = "GetPreparedEncounter";
    private const int MaximumMonsterQuantity = 100;

    internal static void MapEncounterEndpoints(this WebApplication application)
    {
        var encounters = application.MapGroup("/api/encounters")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier))
            .ProtectCookieRequests();
        encounters.MapGet("", ListAsync);
        encounters.MapGet("/{id:guid}", GetAsync).WithName(DetailRoute);
        encounters.MapPost("", CreateAsync);
        encounters.MapPut("/{id:guid}", RenameAsync);
        encounters.MapPost("/{id:guid}/participants", AddParticipantsAsync);
        encounters.MapPut("/{id:guid}/participants/{participantId:guid}/color", ChangeParticipantColorAsync);
        encounters.MapDelete("/{id:guid}/participants/{participantId:guid}", RemoveParticipantAsync);
    }

    private static async Task<IResult> ListAsync(HttpContext context, PathfinderDbContext database)
    {
        var summaries = await OwnedEncounters(context, database).OrderBy(encounter => encounter.Name)
            .ThenBy(encounter => encounter.Id)
            .Select(encounter => new { encounter.Id, encounter.Name, ParticipantCount = encounter.Participants.Count })
            .ToListAsync(context.RequestAborted);
        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetAsync(Guid id, HttpContext context, PathfinderDbContext database)
    {
        var representation = await ReadAsync(id, context, database);
        if (representation is null)
        {
            return Results.NotFound();
        }

        context.Response.Headers.ETag = EntityTag(representation);
        return Results.Ok(representation);
    }

    private static async Task<IResult> CreateAsync(NameRequest request, HttpContext context, PathfinderDbContext database)
    {
        if (ValidateName(request) is { } error)
        {
            return error;
        }

        var encounter = new PreparedEncounter(OwnerId(context), request.Name!);
        database.PreparedEncounters.Add(encounter);
        await database.SaveChangesAsync(context.RequestAborted);
        var representation = new EncounterResponse(encounter.Id, encounter.Name, encounter.Version, []);
        context.Response.Headers.ETag = EntityTag(representation);
        return Results.CreatedAtRoute(DetailRoute, new { id = encounter.Id }, representation);
    }

    private static async Task<IResult> RenameAsync(Guid id, NameRequest request, HttpContext context,
        PathfinderDbContext database)
    {
        if (ValidateName(request) is { } nameError)
        {
            return nameError;
        }

        var (encounter, error) = await FindForChangeAsync(id, context, database);
        if (encounter is null)
        {
            return error!;
        }

        encounter.Rename(request.Name!);
        if (!await SaveAsync(database, context.RequestAborted))
        {
            return VersionConflict();
        }

        return await GetAsync(id, context, database);
    }

    private static async Task<IResult> AddParticipantsAsync(Guid id, AddParticipantsRequest request, HttpContext context,
        PathfinderDbContext database)
    {
        var isMonster = request.Kind == "monsters";
        var quantity = request.Quantity ?? 1;
        var errors = new Dictionary<string, string[]>();
        if (!isMonster && request.Kind != "characters")
        {
            errors["kind"] = ["invalidKind"];
        }
        if (request.TemplateId == Guid.Empty)
        {
            errors["templateId"] = ["required"];
        }
        if (quantity < 1 || quantity > (isMonster ? MaximumMonsterQuantity : 1))
        {
            errors["quantity"] = ["invalidQuantity"];
        }
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var (encounter, error) = await FindForChangeAsync(id, context, database);
        if (encounter is null)
        {
            return error!;
        }

        var cancellationToken = context.RequestAborted;
        var ownerId = OwnerId(context);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        // Keep the selected template active and owned through insertion, even during an archive or delete request.
        var activeTemplate = isMonster
            ? database.Database.SqlQuery<Guid>($"""
                SELECT id FROM monster_templates WHERE id = {request.TemplateId} AND owner_user_id = {ownerId}
                    AND NOT is_archived FOR SHARE
                """)
            : database.Database.SqlQuery<Guid>($"""
                SELECT id FROM character_templates WHERE id = {request.TemplateId} AND owner_user_id = {ownerId}
                    AND NOT is_archived FOR SHARE
                """);
        if ((await activeTemplate.ToListAsync(cancellationToken)).Count == 0)
        {
            return Results.NotFound(new { code = "templateUnavailable" });
        }

        var participants = encounter.CreateParticipants(request.TemplateId, isMonster, quantity);
        // Reserve the numbers with the encounter version check before inserting the rows.
        if (!await SaveAsync(database, cancellationToken))
        {
            return VersionConflict();
        }

        database.EncounterParticipants.AddRange(participants);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, context, database);
    }

    private static Task<IResult> ChangeParticipantColorAsync(Guid id, Guid participantId, ParticipantColorRequest request,
        HttpContext context, PathfinderDbContext database)
    {
        var color = EncounterParticipant.NormalizeColor(request.Color);
        if (color is null)
        {
            return Task.FromResult<IResult>(Results.ValidationProblem(new Dictionary<string, string[]> { ["color"] = ["invalidColor"] }));
        }

        return ChangeParticipantAsync(id, participantId, context, database, participant => participant.ChangeColor(color));
    }

    private static Task<IResult> RemoveParticipantAsync(Guid id, Guid participantId, HttpContext context,
        PathfinderDbContext database)
    {
        return ChangeParticipantAsync(id, participantId, context, database, participant => database.EncounterParticipants.Remove(participant));
    }

    private static async Task<IResult> ChangeParticipantAsync(Guid id, Guid participantId, HttpContext context,
        PathfinderDbContext database, Action<EncounterParticipant> change)
    {
        var (encounter, error) = await FindForChangeAsync(id, context, database);
        if (encounter is null)
        {
            return error!;
        }

        var participant = await database.EncounterParticipants.SingleOrDefaultAsync(
            entry => entry.EncounterId == encounter.Id && entry.Id == participantId, context.RequestAborted);
        if (participant is null)
        {
            return Results.NotFound();
        }

        change(participant);
        encounter.MarkChanged();
        if (!await SaveAsync(database, context.RequestAborted))
        {
            return VersionConflict();
        }

        return await GetAsync(id, context, database);
    }

    private static IQueryable<PreparedEncounter> OwnedEncounters(HttpContext context, PathfinderDbContext database)
    {
        var ownerId = OwnerId(context);
        return database.PreparedEncounters.Where(encounter => encounter.OwnerUserId == ownerId);
    }

    private static string OwnerId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private static Task<EncounterResponse?> ReadAsync(Guid id, HttpContext context, PathfinderDbContext database)
    {
        return OwnedEncounters(context, database).Where(encounter => encounter.Id == id)
            .Select(encounter => new EncounterResponse(encounter.Id, encounter.Name, encounter.Version,
                encounter.Participants.OrderBy(entry => entry.Number).Select(entry => new ParticipantResponse(
                    entry.Id,
                    entry.Number,
                    entry.Color,
                    entry.CharacterTemplateId != null ? "characters" : "monsters",
                    (entry.CharacterTemplateId ?? entry.MonsterTemplateId)!.Value,
                    entry.CharacterTemplateId != null ? entry.CharacterTemplate!.Name : entry.MonsterTemplate!.Name,
                    entry.CharacterTemplateId != null ? entry.CharacterTemplate!.Statistics.MaxHitPoints : entry.MonsterTemplate!.Statistics.MaxHitPoints,
                    entry.CharacterTemplateId != null ? entry.CharacterTemplate!.IsArchived : entry.MonsterTemplate!.IsArchived
                )).ToArray()))
            .SingleOrDefaultAsync(context.RequestAborted);
    }

    private static async Task<(PreparedEncounter? Encounter, IResult? Error)> FindForChangeAsync(
        Guid id, HttpContext context, PathfinderDbContext database)
    {
        var representation = await ReadAsync(id, context, database);
        if (representation is null)
        {
            return (null, Results.NotFound());
        }

        if (!context.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatch))
        {
            return (null, Results.StatusCode(StatusCodes.Status428PreconditionRequired));
        }
        if (ifMatch.Count != 1 || !string.Equals(ifMatch[0]?.Trim(), EntityTag(representation), StringComparison.Ordinal))
        {
            return (null, VersionConflict());
        }

        var encounter = await OwnedEncounters(context, database).SingleOrDefaultAsync(
            encounter => encounter.Id == id && encounter.Version == representation.Version, context.RequestAborted);
        return encounter is null ? (null, VersionConflict()) : (encounter, null);
    }

    private static async Task<bool> SaveAsync(PathfinderDbContext database, CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static IResult? ValidateName(NameRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        LibraryValidation.Text(request.Name, "name", errors, 256);
        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static string EntityTag(EncounterResponse representation)
    {
        var contentHash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(representation));
        return $"\"{representation.Version:N}-{Convert.ToHexString(contentHash)}\"";
    }

    private static IResult VersionConflict()
    {
        return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed record NameRequest(string? Name);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    internal sealed record AddParticipantsRequest(string? Kind, Guid TemplateId, int? Quantity);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed record ParticipantColorRequest(string? Color);

    internal sealed record EncounterResponse(Guid Id, string Name, [property: JsonIgnore] Guid Version,
        ParticipantResponse[] Participants);

    internal sealed record ParticipantResponse(Guid Id, int Number, string Color, string Kind, Guid TemplateId, string Name, int MaxHitPoints,
        bool IsArchived);
}
