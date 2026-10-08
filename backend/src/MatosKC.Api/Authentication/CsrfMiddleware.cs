using Microsoft.AspNetCore.Antiforgery;
namespace MatosKC.Api.Authentication;

public sealed class CsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments("/api") &&
            !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method))
        {
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException)
            {
                await Results.Problem(statusCode: 400, title: "Invalid CSRF token",
                    detail: "Fetch /api/auth/csrf and send the token in X-CSRF-TOKEN.").ExecuteAsync(context);
                return;
            }
        }
        await next(context);
    }
}
