using Microsoft.AspNetCore.Antiforgery;

namespace PathfinderElements.Api.Access;

internal static class CookieApiSecurity
{
    internal static RouteGroupBuilder ProtectCookieRequests(this RouteGroupBuilder group)
    {
        return group.AddEndpointFilter(async (context, next) =>
        {
            var request = context.HttpContext.Request;
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            if (!request.IsHttps)
            {
                return Results.BadRequest();
            }

            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method))
            {
                var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
                try
                {
                    await antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.BadRequest();
                }
            }

            return await next(context);
        });
    }
}
