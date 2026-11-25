using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Npgsql;
using PathfinderElements.Api.Access;
using PathfinderElements.Api.Persistence;

namespace PathfinderElements.Api.Library;

internal static class LibraryEndpoints
{
    internal static void MapLibraryEndpoints(this WebApplication application)
    {
        var library = application.MapGroup("/api")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier))
            .ProtectCookieRequests();
        MapTemplates<CharacterTemplate>(library.MapGroup("/characters"), "GetCharacterTemplate",
            (owner, name, statistics) => new CharacterTemplate(owner, name, statistics));
        MapTemplates<MonsterTemplate>(library.MapGroup("/monsters"), "GetMonsterTemplate",
            (owner, name, statistics) => new MonsterTemplate(owner, name, statistics));
    }

    private static void MapTemplates<TTemplate>(
        RouteGroupBuilder group, string itemRouteName, Func<string, string, CombatStatistics, TTemplate> create)
        where TTemplate : class, ILibraryTemplate
    {
        group.MapGet("", ListAsync<TTemplate>);
        group.MapGet("/{id:guid}", GetAsync<TTemplate>).WithName(itemRouteName);
        group.MapPost("", async (TemplateWriteRequest request, HttpContext context, PathfinderDbContext database,
            CancellationToken cancellationToken) =>
        {
            var errors = request.Validate();
            if (errors.Count != 0)
            {
                return Results.ValidationProblem(errors);
            }

            var template = create(OwnerId(context), request.Name!, request.Statistics!.ToDomain());
            database.Set<TTemplate>().Add(template);
            await database.SaveChangesAsync(cancellationToken);
            var response = WriteRepresentation(context, template);
            return Results.CreatedAtRoute(itemRouteName, new { id = template.Id }, response);
        });

        group.MapPut("/{id:guid}", (Guid id, TemplateWriteRequest request, HttpContext context, PathfinderDbContext database,
            CancellationToken cancellationToken) =>
        {
            var errors = request.Validate();
            if (errors.Count != 0)
            {
                return Task.FromResult(Results.ValidationProblem(errors));
            }

            return ChangeAsync<TTemplate>(id, context, database,
                template => template.Update(request.Name!, request.Statistics!.ToDomain()), cancellationToken);
        });
        group.MapPut("/{id:guid}/archive", (Guid id, TemplateArchiveRequest request, HttpContext context,
            PathfinderDbContext database, CancellationToken cancellationToken) =>
        {
            return ChangeAsync<TTemplate>(id, context, database, template => template.SetArchived(request.IsArchived),
                cancellationToken);
        });
        group.MapDelete("/{id:guid}", DeleteAsync<TTemplate>);
    }

    private static async Task<IResult> ListAsync<TTemplate>(bool? includeArchived, HttpContext context,
        PathfinderDbContext database, CancellationToken cancellationToken)
        where TTemplate : class, ILibraryTemplate
    {
        var templates = OwnedTemplates<TTemplate>(database, OwnerId(context)).AsNoTracking();
        if (includeArchived != true)
        {
            templates = templates.Where(template => !template.IsArchived);
        }

        var response = await templates.OrderBy(template => template.Name).ThenBy(template => template.Id)
            .Select(template => new TemplateResponse(template.Id, template.Name, template.Statistics, template.IsArchived))
            .ToListAsync(cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetAsync<TTemplate>(Guid id, HttpContext context, PathfinderDbContext database,
        CancellationToken cancellationToken)
        where TTemplate : class, ILibraryTemplate
    {
        var template = await OwnedTemplates<TTemplate>(database, OwnerId(context)).AsNoTracking()
            .SingleOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (template is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(WriteRepresentation(context, template));
    }

    private static async Task<IResult> ChangeAsync<TTemplate>(Guid id, HttpContext context, PathfinderDbContext database,
        Action<TTemplate> change, CancellationToken cancellationToken)
        where TTemplate : class, ILibraryTemplate
    {
        var (template, error) = await FindForChangeAsync<TTemplate>(id, context, database, cancellationToken);
        if (template is null)
        {
            return error!;
        }

        change(template);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
        }

        return Results.Ok(WriteRepresentation(context, template));
    }

    private static async Task<IResult> DeleteAsync<TTemplate>(Guid id, HttpContext context, PathfinderDbContext database,
        CancellationToken cancellationToken)
        where TTemplate : class, ILibraryTemplate
    {
        var (template, error) = await FindForChangeAsync<TTemplate>(id, context, database, cancellationToken);
        if (template is null)
        {
            return error!;
        }

        database.Set<TTemplate>().Remove(template);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Results.Conflict(new { code = "templateReferenced" });
        }

        return Results.NoContent();
    }

    private static async Task<(TTemplate? Template, IResult? Error)> FindForChangeAsync<TTemplate>(
        Guid id, HttpContext context, PathfinderDbContext database, CancellationToken cancellationToken)
        where TTemplate : class, ILibraryTemplate
    {
        var template = await OwnedTemplates<TTemplate>(database, OwnerId(context))
            .SingleOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (template is null)
        {
            return (null, Results.NotFound());
        }

        if (!context.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatch))
        {
            return (null, Results.StatusCode(StatusCodes.Status428PreconditionRequired));
        }

        if (ifMatch.Count != 1 || !string.Equals(ifMatch[0]?.Trim(), EntityTag(template.Version), StringComparison.Ordinal))
        {
            return (null, Results.StatusCode(StatusCodes.Status412PreconditionFailed));
        }

        return (template, null);
    }

    private static IQueryable<TTemplate> OwnedTemplates<TTemplate>(PathfinderDbContext database, string ownerUserId)
        where TTemplate : class, ILibraryTemplate
    {
        return database.Set<TTemplate>().Where(template => template.OwnerUserId == ownerUserId);
    }

    private static string OwnerId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private static TemplateResponse WriteRepresentation(HttpContext context, ILibraryTemplate template)
    {
        context.Response.Headers.ETag = EntityTag(template.Version);
        return new TemplateResponse(template.Id, template.Name, template.Statistics, template.IsArchived);
    }

    private static string EntityTag(Guid version)
    {
        return $"\"{version:N}\"";
    }
}
