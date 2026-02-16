using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Groups;
using LoopMeet.Api.Services.Invitations;

namespace LoopMeet.Api.Endpoints;

public static class GroupsEndpoints
{
    public static IEndpointRouteBuilder MapGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/groups", async (
                CurrentUserService currentUser,
                GroupQueryService groupQueryService,
                InvitationQueryService invitationQueryService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var response = await groupQueryService.GetGroupsAsync(userId.Value, cancellationToken);
                var email = currentUser.Email;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    response.PendingInvitations = await invitationQueryService.ListPendingAsync(email, cancellationToken);
                }
                return Results.Ok(response);
            })
            .RequireAuthorization();

        app.MapPost("/groups", async (
                CreateGroupRequest request,
                CurrentUserService currentUser,
                GroupCommandService groupCommandService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await groupCommandService.CreateAsync(userId.Value, request.Name, cancellationToken);
                return result.Status switch
                {
                    GroupCommandStatus.Success => Results.Created($"/groups/{result.Group!.Id}", result.Group),
                    GroupCommandStatus.DuplicateName => Results.Json(new ErrorResponse
                    {
                        Code = "duplicate_group_name",
                        Message = "You already have a group with that name."
                    }, statusCode: StatusCodes.Status409Conflict),
                    GroupCommandStatus.InvalidName => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_group_name",
                        Message = "Please provide a group name."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapGet("/groups/{groupId:guid}", async (
                Guid groupId,
                GroupQueryService groupQueryService,
                CancellationToken cancellationToken) =>
            {
                var response = await groupQueryService.GetGroupDetailAsync(groupId, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .RequireAuthorization();

        app.MapPatch("/groups/{groupId:guid}", async (
                Guid groupId,
                UpdateGroupRequest request,
                CurrentUserService currentUser,
                GroupCommandService groupCommandService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await groupCommandService.RenameAsync(groupId, userId.Value, request.Name, cancellationToken);
                return result.Status switch
                {
                    GroupCommandStatus.Success => Results.Ok(result.Group),
                    GroupCommandStatus.NotFound => Results.NotFound(),
                    GroupCommandStatus.Forbidden => Results.Json(new ErrorResponse
                    {
                        Code = "not_group_owner",
                        Message = "Only the group owner can edit this group."
                    }, statusCode: StatusCodes.Status403Forbidden),
                    GroupCommandStatus.DuplicateName => Results.Json(new ErrorResponse
                    {
                        Code = "duplicate_group_name",
                        Message = "You already have a group with that name."
                    }, statusCode: StatusCodes.Status409Conflict),
                    GroupCommandStatus.InvalidName => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_group_name",
                        Message = "Please provide a group name."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        return app;
    }
}
