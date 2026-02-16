using LoopMeet.App.Features.Invitations.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IInvitationsApi
{
    [Get("/invitations")]
    Task<InvitationsResponse> GetInvitationsAsync();
}

public sealed class InvitationsApi
{
    private readonly IInvitationsApi _api;

    public InvitationsApi(IInvitationsApi api)
    {
        _api = api;
    }

    public Task<InvitationsResponse> GetInvitationsAsync() => _api.GetInvitationsAsync();
}
