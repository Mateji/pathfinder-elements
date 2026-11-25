using Microsoft.EntityFrameworkCore;
using Npgsql;
using PathfinderElements.Api.Persistence;

namespace PathfinderElements.Api.Library;

internal static class LibraryImportEndpoint
{
    internal const int MaximumRequestBytes = 1024 * 1024;

    internal static async Task<IResult> ImportAsync(HttpRequest request, string ownerUserId, PathfinderDbContext database)
    {
        if (request.ContentLength > MaximumRequestBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!request.HasJsonContentType())
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var cancellationToken = request.HttpContext.RequestAborted;
        var (document, errors) = await LibraryImportDocument.ReadAsync(request.Body, cancellationToken);
        if (document is null)
        {
            return Results.ValidationProblem(errors);
        }

        var characters = (document.Characters ?? []).Select(character => character!.ToCharacterTemplate(ownerUserId)).ToArray();
        var monsters = (document.Monsters ?? []).Select(monster => monster!.ToMonsterTemplate(ownerUserId)).ToArray();
        var receipt = new LibraryImportReceipt(ownerUserId, document.ComputeFingerprint());

        database.Set<LibraryImportReceipt>().Add(receipt);
        database.CharacterTemplates.AddRange(characters);
        database.MonsterTemplates.AddRange(monsters);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: LibraryImportReceipt.PrimaryKeyName
            })
        {
            return Results.Conflict(new { code = "importAlreadyProcessed" });
        }

        return Results.Json(new
        {
            document.SchemaVersion,
            CharacterIds = characters.Select(character => character.Id).ToArray(),
            MonsterIds = monsters.Select(monster => monster.Id).ToArray()
        }, statusCode: StatusCodes.Status201Created);
    }
}
