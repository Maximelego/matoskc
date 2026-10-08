using Microsoft.AspNetCore.Antiforgery;

namespace MatosKC.Api.Authentication;

/// <summary>
/// Marks endpoints that accept changes authenticated by browser cookies.
/// </summary>
public sealed class RequireCsrfValidation
{
}

public sealed class CsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        bool requiresValidation =
            context.GetEndpoint()?.Metadata.GetMetadata<RequireCsrfValidation>() is not null;

        string method = context.Request.Method;
        bool isReadOnly = HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method);

        if (requiresValidation && !isReadOnly)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                await Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid CSRF token",
                    detail: "Fetch /auth/csrf and send the token in X-CSRF-TOKEN."
                ).ExecuteAsync(context);

                return;
            }
        }

        await next(context);
    }
}
