using System.Security.Claims;
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
        var group = app.MapGroup("/api/accounts").WithTags("Accounts").RequireAuthorization("ManageAccounts");

        group.MapGet("/", async (string? displayName,
                                 string? email,
                                 Role? role,
                                 ListAccountsUseCase useCase,
                                 CancellationToken ct) => Results.Ok(
                new {
                    accounts = await useCase.ExecuteAsync(new() {
                        DisplayName = displayName, Email = email, Role = role
                    }, ct)
                }));
        group.MapGet("/{id:guid}", async (Guid id, GetAccountUseCase useCase, CancellationToken ct) =>
            Results.Ok(AccountResponse.From(await useCase.ExecuteAsync(id, ct))));
        group.MapPost("/", async (CreateAccountDto dto, CreateAccountUseCase useCase, GetAccountUseCase getAccount, CancellationToken ct) =>
        {
            try
            {
                Guid id = await useCase.ExecuteAsync(dto, ct);
                return Results.Created($"/accounts/{id}", AccountResponse.From(await getAccount.ExecuteAsync(id, ct)));
            }
            catch (InvalidOperationException e)
            {
                return Results.Problem(statusCode: e.Message.Contains("already exists") ? 409 : 400,
                    title: "Cannot create account", detail: e.Message);
            }
        });
        group.MapPut("/{id:guid}", async (Guid id, UpdateAccountDto dto, UpdateAccountUseCase useCase, HttpContext context, CancellationToken ct) =>
        {
            if (id.ToString() == context.User.FindFirstValue(ClaimTypes.NameIdentifier) && dto.IsActive == false)
                return Results.Problem(statusCode: 409, title: "Cannot deactivate your own account");
            return Results.Ok(await useCase.ExecuteAsync(id, dto, ct));
        });
        group.MapDelete("/{id:guid}", async (Guid id, DeleteAccountUseCase useCase, HttpContext context, CancellationToken ct) =>
        {
            if (id.ToString() == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Results.Problem(statusCode: 409, title: "Cannot delete your own account");
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
