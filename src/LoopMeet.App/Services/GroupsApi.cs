using LoopMeet.App.Features.Groups.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IGroupsApi
{
    [Get("/groups")]
    Task<GroupsResponse> GetGroupsAsync();

    [Get("/groups/{groupId}")]
    Task<GroupDetail> GetGroupAsync(Guid groupId);

    [Post("/groups")]
    Task<GroupSummary> CreateGroupAsync([Body] CreateGroupRequest request);

    [Patch("/groups/{groupId}")]
    Task<GroupSummary> UpdateGroupAsync(Guid groupId, [Body] UpdateGroupRequest request);
}

public sealed class GroupsApi
{
    private readonly IGroupsApi _api;

    public GroupsApi(IGroupsApi api)
    {
        _api = api;
    }

    public Task<GroupsResponse> GetGroupsAsync() => _api.GetGroupsAsync();

    public Task<GroupDetail> GetGroupAsync(Guid groupId) => _api.GetGroupAsync(groupId);

    public Task<GroupSummary> CreateGroupAsync(CreateGroupRequest request) => _api.CreateGroupAsync(request);

    public Task<GroupSummary> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request) =>
        _api.UpdateGroupAsync(groupId, request);
}
