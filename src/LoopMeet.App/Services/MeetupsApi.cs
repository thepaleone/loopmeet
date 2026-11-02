using LoopMeet.App.Features.Meetups.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IMeetupsApi
{
    [Get("/groups/{groupId}/meetups")]
    Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId);

    [Get("/groups/{groupId}/meetups/past")]
    Task<PastMeetupsResponse> GetPastMeetupsAsync(Guid groupId, [Query] string? cursor = null, [Query] int? limit = null);

    [Get("/groups/{groupId}/meetups/{meetupId}")]
    Task<MeetupSummary> GetMeetupAsync(Guid groupId, Guid meetupId);

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

    public Task<PastMeetupsResponse> GetPastMeetupsAsync(Guid groupId, string? cursor = null) =>
        _api.GetPastMeetupsAsync(groupId, cursor);

    public Task<MeetupSummary> GetMeetupAsync(Guid groupId, Guid meetupId) =>
        _api.GetMeetupAsync(groupId, meetupId);

    public Task<MeetupSummary> CreateMeetupAsync(Guid groupId, CreateMeetupRequest request) =>
        _api.CreateMeetupAsync(groupId, request);

    public Task<MeetupSummary> UpdateMeetupAsync(Guid groupId, Guid meetupId, UpdateMeetupRequest request) =>
        _api.UpdateMeetupAsync(groupId, meetupId, request);

    public Task DeleteMeetupAsync(Guid groupId, Guid meetupId) =>
        _api.DeleteMeetupAsync(groupId, meetupId);

    public Task<UpcomingMeetupsResponse> GetUpcomingMeetupsAsync() =>
        _api.GetUpcomingMeetupsAsync();
}
