using System.Security.Claims;
using MatosKC.Api.Authentication;
using MatosKC.Application.Accounts.Create;
using MatosKC.Application.Accounts.Delete;
using MatosKC.Application.Accounts.Get;
using MatosKC.Application.Accounts.List;
using MatosKC.Application.Accounts.Update;
using MatosKC.Domain.Entities.Accounts;

namespace MatosKC.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/accounts")
            .WithTags("Accounts")
            .RequireAuthorization("ManageAccounts")
            .WithMetadata(new RequireCsrfValidation());

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> ListAsync(
        string? displayName,
        string? email,
        Role? role,
        ListAccountsUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        var query = new ListAccountsQuery
        {
            DisplayName = displayName,
            Email = email,
            Role = role
        };

        var accounts = await useCase.ExecuteAsync(query, cancellationToken);

        return Results.Ok(new { accounts });
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        GetAccountUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        var account = await useCase.ExecuteAsync(id, cancellationToken);

        return Results.Ok(AccountResponse.From(account));
    }

    private static async Task<IResult> CreateAsync(
        CreateAccountDto dto,
        CreateAccountUseCase useCase,
        GetAccountUseCase getAccount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            Guid id = await useCase.ExecuteAsync(dto, cancellationToken);
            var account = await getAccount.ExecuteAsync(id, cancellationToken);

            return Results.Created($"/accounts/{id}", AccountResponse.From(account));
        }
        catch (InvalidOperationException exception)
        {
            int statusCode = exception.Message.Contains("already exists") ? 409 : 400;

            return Results.Problem(
                statusCode: statusCode,
                title: "Cannot create account",
                detail: exception.Message
            );
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateAccountDto dto,
        UpdateAccountUseCase useCase,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        bool isCurrentAccount = id.ToString()
            == context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (isCurrentAccount && dto.IsActive == false)
        {
            return Results.Problem(statusCode: 409, title: "Cannot deactivate your own account");
        }

        var account = await useCase.ExecuteAsync(id, dto, cancellationToken);

        return Results.Ok(account);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteAccountUseCase useCase,
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        bool isCurrentAccount = id.ToString()
            == context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (isCurrentAccount)
        {
            return Results.Problem(statusCode: 409, title: "Cannot delete your own account");
        }

        await useCase.ExecuteAsync(id, cancellationToken);

        return Results.NoContent();
    }
}
