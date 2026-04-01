using LoopMeet.App.Features.Meetups.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IMeetupsApi
{
    [Get("/groups/{groupId}/meetups")]
    Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId);

    [Post("/groups/{groupId}/meetups")]
    Task<MeetupSummary> CreateMeetupAsync(Guid groupId, [Body] CreateMeetupRequest request);

    [Patch("/groups/{groupId}/meetups/{meetupId}")]
    Task<MeetupSummary> UpdateMeetupAsync(Guid groupId, Guid meetupId, [Body] UpdateMeetupRequest request);

    [Delete("/groups/{groupId}/meetups/{meetupId}")]
    Task DeleteMeetupAsync(Guid groupId, Guid meetupId);

    [Get("/meetups/upcoming")]
    Task<UpcomingMeetupsResponse> GetUpcomingMeetupsAsync();
}

public sealed class MeetupsApi
{
    private readonly IMeetupsApi _api;

    public MeetupsApi(IMeetupsApi api)
    {
        _api = api;
    }

    public Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId) =>
        _api.GetGroupMeetupsAsync(groupId);

    public Task<MeetupSummary> CreateMeetupAsync(Guid groupId, CreateMeetupRequest request) =>
        _api.CreateMeetupAsync(groupId, request);

    public Task<MeetupSummary> UpdateMeetupAsync(Guid groupId, Guid meetupId, UpdateMeetupRequest request) =>
        _api.UpdateMeetupAsync(groupId, meetupId, request);

    public Task DeleteMeetupAsync(Guid groupId, Guid meetupId) =>
        _api.DeleteMeetupAsync(groupId, meetupId);

    public Task<UpcomingMeetupsResponse> GetUpcomingMeetupsAsync() =>
        _api.GetUpcomingMeetupsAsync();
}
