using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Accounts;
using LoopMeet.Api.Services.Auth;

namespace LoopMeet.Api.Endpoints;

public static class AccountDeletionEndpoints
{
    public static IEndpointRouteBuilder MapAccountDeletionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/account-deletion", async (
                CurrentUserService currentUser,
                AccountDeletionService deletionService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await deletionService.GetStatusAsync(userId.Value, cancellationToken));
            })
            .RequireAuthorization();

        app.MapGet("/users/account-deletion/preview", async (
                CurrentUserService currentUser,
                AccountDeletionService deletionService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await deletionService.GetPreviewAsync(userId.Value, cancellationToken));
            })
            .RequireAuthorization();

        // The body is optional so a missing body gets the same invalid_confirmation error as a wrong one.
        app.MapPost("/users/account-deletion", async (
                AccountDeletionRequest? request,
                CurrentUserService currentUser,
                AccountDeletionService deletionService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await deletionService.RequestAsync(userId.Value, request?.Confirmation, cancellationToken);
                return result.Status switch
                {
                    AccountDeletionCommandStatus.Success => Results.Json(
                        deletionService.ToResponse(result.Deletion),
                        statusCode: StatusCodes.Status202Accepted),
                    AccountDeletionCommandStatus.InvalidConfirmation => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_confirmation",
                        Message = "Type DELETE to confirm."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    AccountDeletionCommandStatus.DeletionInProgress => Results.Json(new ErrorResponse
                    {
                        Code = "deletion_in_progress",
                        Message = "This account is already being deleted."
                    }, statusCode: StatusCodes.Status409Conflict),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        return app;
    }
}
