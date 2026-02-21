using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Invitations;

namespace LoopMeet.Api.Endpoints;

public static class InvitationsEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/invitations", async (
                CurrentUserService currentUser,
                InvitationQueryService invitationQueryService,
                ILogger<InvitationsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var email = currentUser.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    logger.LogWarning("List invitations unauthorized: missing email");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Listing invitations for current user");
                var invitations = await invitationQueryService.ListPendingAsync(email, cancellationToken);
                logger.LogInformation("Listed invitations count={Count}", invitations.Count);
                return Results.Ok(new Contracts.InvitationsResponse { Invitations = invitations });
            })
            .RequireAuthorization();

        app.MapPost("/groups/{groupId:guid}/invitations", async (
                Guid groupId,
                CreateInvitationRequest request,
                CurrentUserService currentUser,
                InvitationCommandService invitationCommandService,
                ILogger<InvitationsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("Create invitation unauthorized: missing user id for {GroupId}", groupId);
                    return Results.Unauthorized();
                }

                logger.LogInformation("Creating invitation for {GroupId} by {UserId}", groupId, userId);
                var result = await invitationCommandService.CreateAsync(userId.Value, groupId, request.Email, cancellationToken);
                logger.LogInformation("Create invitation result {Status} for {GroupId} by {UserId}", result.Status, groupId, userId);
                return result.Status switch
                {
                    InvitationCommandStatus.Success => Results.Created($"/invitations/{result.Invitation!.Id}", result.Invitation),
                    InvitationCommandStatus.NotFound => Results.NotFound(),
                    InvitationCommandStatus.Forbidden => Results.Json(new ErrorResponse
                    {
                        Code = "not_group_owner",
                        Message = "Only the group owner can invite members."
                    }, statusCode: StatusCodes.Status403Forbidden),
                    InvitationCommandStatus.AlreadyMember => Results.Json(new ErrorResponse
                    {
                        Code = "already_member",
                        Message = "That user is already in the group."
                    }, statusCode: StatusCodes.Status409Conflict),
                    InvitationCommandStatus.Duplicate => Results.Json(new ErrorResponse
                    {
                        Code = "invitation_exists",
                        Message = "An invitation is already pending for that email."
                    }, statusCode: StatusCodes.Status409Conflict),
                    InvitationCommandStatus.InvalidEmail => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_email",
                        Message = "Please provide a valid email address."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapPost("/invitations/{invitationId:guid}/accept", async (
                Guid invitationId,
                CurrentUserService currentUser,
                InvitationCommandService invitationCommandService,
                ILogger<InvitationsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                var email = currentUser.Email;
                if (userId is null || string.IsNullOrWhiteSpace(email))
                {
                    logger.LogWarning("Accept invitation unauthorized: missing user/email for {InvitationId}", invitationId);
                    return Results.Unauthorized();
                }

                logger.LogInformation("Accepting invitation {InvitationId} by {UserId}", invitationId, userId);
                var result = await invitationCommandService.AcceptAsync(userId.Value, email, invitationId, cancellationToken);
                logger.LogInformation("Accept invitation result {Status} for {InvitationId} by {UserId}", result.Status, invitationId, userId);
                return result.Status switch
                {
                    InvitationCommandStatus.Success => Results.Ok(result.Invitation),
                    InvitationCommandStatus.NotFound => Results.NotFound(),
                    InvitationCommandStatus.AlreadyMember => Results.Json(new ErrorResponse
                    {
                        Code = "already_member",
                        Message = "You are already in this group."
                    }, statusCode: StatusCodes.Status409Conflict),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapPost("/invitations/{invitationId:guid}/decline", async (
                Guid invitationId,
                CurrentUserService currentUser,
                InvitationCommandService invitationCommandService,
                ILogger<InvitationsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                var email = currentUser.Email;
                if (userId is null || string.IsNullOrWhiteSpace(email))
                {
                    logger.LogWarning("Decline invitation unauthorized: missing user/email for {InvitationId}", invitationId);
                    return Results.Unauthorized();
                }

                logger.LogInformation("Declining invitation {InvitationId} by {UserId}", invitationId, userId);
                var result = await invitationCommandService.DeclineAsync(userId.Value, email, invitationId, cancellationToken);
                logger.LogInformation("Decline invitation result {Status} for {InvitationId} by {UserId}", result.Status, invitationId, userId);
                return result.Status switch
                {
                    InvitationCommandStatus.Success => Results.Ok(result.Invitation),
                    InvitationCommandStatus.NotFound => Results.NotFound(),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        return app;
    }

    private sealed class LogMarker
    {
    }
}
