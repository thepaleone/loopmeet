using LoopMeet.App.Features.Invitations.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IInvitationsApi
{
    [Get("/invitations")]
    Task<InvitationsResponse> GetInvitationsAsync();

    [Post("/groups/{groupId}/invitations")]
    Task<InvitationSummary> CreateInvitationAsync(Guid groupId, [Body] CreateInvitationRequest request);

    [Post("/invitations/{invitationId}/accept")]
    Task<InvitationSummary> AcceptInvitationAsync(Guid invitationId);
}

public sealed class InvitationsApi
{
    private readonly IInvitationsApi _api;

    public InvitationsApi(IInvitationsApi api)
    {
        _api = api;
    }

    public Task<InvitationsResponse> GetInvitationsAsync() => _api.GetInvitationsAsync();

    public Task<InvitationSummary> CreateInvitationAsync(Guid groupId, CreateInvitationRequest request) =>
        _api.CreateInvitationAsync(groupId, request);

    public Task<InvitationSummary> AcceptInvitationAsync(Guid invitationId) =>
        _api.AcceptInvitationAsync(invitationId);
}
