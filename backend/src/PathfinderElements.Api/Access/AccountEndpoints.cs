using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace PathfinderElements.Api.Access;

internal static class AccountEndpoints
{
    internal static void MapAccountEndpoints(this WebApplication application)
    {
        var accounts = application.MapGroup("/api/auth")
            .AddEndpointFilter(async (context, next) =>
            {
                var request = context.HttpContext.Request;
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                if (!request.IsHttps)
                {
                    return Results.BadRequest();
                }

                if (HttpMethods.IsPost(request.Method))
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

        accounts.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        }).AllowAnonymous().RequireRateLimiting("auth-csrf");

        accounts.MapPost("/login", async (LoginRequest request, SignInManager<IdentityUser> signIn) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 256 ||
                string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
            {
                return Results.BadRequest();
            }

            var result = await signIn.PasswordSignInAsync(request.Username, request.Password, isPersistent: true, lockoutOnFailure: true);
            return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
        }).AllowAnonymous().RequireRateLimiting("auth-login");

        accounts.MapGet("/session", (HttpContext context, UserManager<IdentityUser> users) =>
        {
            return Results.Ok(new { userId = users.GetUserId(context.User), username = users.GetUserName(context.User) });
        });

        accounts.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Results.NoContent();
        });
    }

    internal sealed record LoginRequest(string? Username, string? Password);
}
