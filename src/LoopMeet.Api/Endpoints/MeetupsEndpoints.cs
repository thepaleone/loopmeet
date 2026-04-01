using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Meetups;

namespace LoopMeet.Api.Endpoints;

public static class MeetupsEndpoints
{
    public static IEndpointRouteBuilder MapMeetupsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId:guid}/meetups", async (
                Guid groupId,
                MeetupQueryService meetupQueryService,
                ILogger<MeetupsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                logger.LogInformation("Listing meetups for group {GroupId}", groupId);
                var response = await meetupQueryService.GetGroupMeetupsAsync(groupId, cancellationToken);
                logger.LogInformation("Listed meetups for group {GroupId} count={Count}", groupId, response.Meetups.Count);
                return Results.Ok(response);
            })
            .RequireAuthorization();

        app.MapPost("/groups/{groupId:guid}/meetups", async (
                Guid groupId,
                CreateMeetupRequest request,
                CurrentUserService currentUser,
                MeetupCommandService meetupCommandService,
                ILogger<MeetupsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("Create meetup unauthorized: missing user id for group {GroupId}", groupId);
                    return Results.Unauthorized();
                }

                logger.LogInformation("Creating meetup for group {GroupId} by {UserId}", groupId, userId);
                var result = await meetupCommandService.CreateAsync(userId.Value, groupId, request, cancellationToken);
                logger.LogInformation("Create meetup result {Status} for group {GroupId} by {UserId}", result.Status, groupId, userId);
                return result.Status switch
                {
                    MeetupCommandStatus.Success => Results.Created($"/groups/{groupId}/meetups/{result.Meetup!.Id}", result.Meetup),
                    MeetupCommandStatus.InvalidTitle => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_title",
                        Message = "Please provide a title (max 200 characters)."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    MeetupCommandStatus.InvalidSchedule => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_schedule",
                        Message = "Scheduled time must be in the future."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    MeetupCommandStatus.InvalidLocation => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_location",
                        Message = "When a place name is provided, latitude and longitude are required."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapPatch("/groups/{groupId:guid}/meetups/{meetupId:guid}", async (
                Guid groupId,
                Guid meetupId,
                UpdateMeetupRequest request,
                MeetupCommandService meetupCommandService,
                ILogger<MeetupsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                logger.LogInformation("Updating meetup {MeetupId} in group {GroupId}", meetupId, groupId);
                var result = await meetupCommandService.UpdateAsync(groupId, meetupId, request, cancellationToken);
                logger.LogInformation("Update meetup result {Status} for {MeetupId} in group {GroupId}", result.Status, meetupId, groupId);
                return result.Status switch
                {
                    MeetupCommandStatus.Success => Results.Ok(result.Meetup),
                    MeetupCommandStatus.NotFound => Results.NotFound(),
                    MeetupCommandStatus.InvalidTitle => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_title",
                        Message = "Please provide a title (max 200 characters)."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    MeetupCommandStatus.InvalidSchedule => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_schedule",
                        Message = "Scheduled time must be in the future."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    MeetupCommandStatus.InvalidLocation => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_meetup_location",
                        Message = "When a place name is provided, latitude and longitude are required."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapDelete("/groups/{groupId:guid}/meetups/{meetupId:guid}", async (
                Guid groupId,
                Guid meetupId,
                MeetupCommandService meetupCommandService,
                ILogger<MeetupsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                logger.LogInformation("Deleting meetup {MeetupId} from group {GroupId}", meetupId, groupId);
                var result = await meetupCommandService.DeleteAsync(groupId, meetupId, cancellationToken);
                logger.LogInformation("Delete meetup result {Status} for {MeetupId} in group {GroupId}", result.Status, meetupId, groupId);
                return result.Status switch
                {
                    MeetupCommandStatus.Success => Results.NoContent(),
                    MeetupCommandStatus.NotFound => Results.NotFound(),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapGet("/meetups/upcoming", async (
                CurrentUserService currentUser,
                MeetupQueryService meetupQueryService,
                ILogger<MeetupsEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("Get upcoming meetups unauthorized: missing user id");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Loading upcoming meetups for {UserId}", userId);
                var response = await meetupQueryService.GetUpcomingForUserAsync(userId.Value, cancellationToken);
                logger.LogInformation("Loaded upcoming meetups for {UserId} count={Count}", userId, response.Meetups.Count);
                return Results.Ok(response);
            })
            .RequireAuthorization();

        return app;
    }

    private sealed class LogMarker
    {
    }
}
