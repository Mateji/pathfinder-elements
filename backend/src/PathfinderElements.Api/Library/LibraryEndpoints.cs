using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Npgsql;
using PathfinderElements.Api.Access;
using PathfinderElements.Api.Persistence;

namespace PathfinderElements.Api.Library;

internal static class LibraryEndpoints
{
    private const string CharacterDetailRoute = "GetCharacterTemplate";
    private const string MonsterDetailRoute = "GetMonsterTemplate";

    internal static void MapLibraryEndpoints(this WebApplication application)
    {
        var library = application.MapGroup("/api")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier))
            .ProtectCookieRequests();
        var characters = library.MapGroup("/characters");
        MapTemplates<CharacterTemplate>(characters, CharacterDetailRoute);
        characters.MapPost("", (TemplateWriteRequest request, HttpContext context, PathfinderDbContext database) =>
        {
            return CreateAsync(request, context, database, request.ToCharacterTemplate, CharacterDetailRoute);
        });
        characters.MapPut("/{id:guid}", (Guid id, TemplateWriteRequest request, HttpContext context, PathfinderDbContext database) =>
        {
            return UpdateAsync<CharacterTemplate>(id, request, context, database,
                template => template.Update(request.Name!, request.Statistics!.ToDomain()));
        });

        var monsters = library.MapGroup("/monsters");
        MapTemplates<MonsterTemplate>(monsters, MonsterDetailRoute);
        monsters.MapPost("", (MonsterWriteRequest request, HttpContext context, PathfinderDbContext database) =>
        {
            return CreateAsync(request, context, database, request.ToMonsterTemplate, MonsterDetailRoute);
        });
        monsters.MapPut("/{id:guid}", (Guid id, MonsterWriteRequest request, HttpContext context, PathfinderDbContext database) =>
        {
            return UpdateAsync<MonsterTemplate>(id, request, context, database,
                template => template.Update(request.Name!, request.Statistics!.ToDomain(), request.ToAttackSequences()));
        });
    }

    private static void MapTemplates<TTemplate>(RouteGroupBuilder group, string itemRouteName)
        where TTemplate : class, ILibraryTemplate
    {
        group.MapGet("", ListAsync<TTemplate>);
        group.MapGet("/{id:guid}", GetAsync<TTemplate>).WithName(itemRouteName);
        group.MapPut("/{id:guid}/archive", (Guid id, TemplateArchiveRequest request, HttpContext context,
            PathfinderDbContext database, CancellationToken cancellationToken) =>
        {
            return ChangeAsync<TTemplate>(id, context, database, template => template.SetArchived(request.IsArchived),
                cancellationToken);
        });
        group.MapDelete("/{id:guid}", DeleteAsync<TTemplate>);
    }

    private static async Task<IResult> CreateAsync<TTemplate>(TemplateWriteRequest request, HttpContext context,
        PathfinderDbContext database, Func<string, TTemplate> create, string itemRouteName)
        where TTemplate : class, ILibraryTemplate
    {
        var errors = request.Validate();
        if (errors.Count != 0)
        {
            return Results.ValidationProblem(errors);
        }

        var template = create(OwnerId(context));
        database.Set<TTemplate>().Add(template);
        await database.SaveChangesAsync(context.RequestAborted);
        var response = WriteRepresentation(context, template);
        return Results.CreatedAtRoute(itemRouteName, new { id = template.Id }, response);
    }

    private static Task<IResult> UpdateAsync<TTemplate>(Guid id, TemplateWriteRequest request, HttpContext context,
        PathfinderDbContext database, Action<TTemplate> update)
        where TTemplate : class, ILibraryTemplate
    {
        var errors = request.Validate();
        if (errors.Count != 0)
        {
            return Task.FromResult(Results.ValidationProblem(errors));
        }

        return ChangeAsync(id, context, database, update, context.RequestAborted);
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
        return new TemplateResponse(template.Id, template.Name, template.Statistics, template.IsArchived,
            (template as MonsterTemplate)?.AttackSequences);
    }

    private static string EntityTag(Guid version)
    {
        return $"\"{version:N}\"";
    }
}
