using LoopMeet.App.Features.Auth.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IUsersApi
{
    [Get("/users/profile")]
    Task<UserProfileResponse> GetProfileAsync();

    [Post("/users/profile")]
    Task<UserProfileRequest> UpsertProfileAsync([Body] UserProfileRequest request);
}

public sealed class UsersApi
{
    private readonly IUsersApi _api;

    public UsersApi(IUsersApi api)
    {
        _api = api;
    }

    public Task<UserProfileResponse> GetProfileAsync() => _api.GetProfileAsync();
    public Task<UserProfileRequest> UpsertProfileAsync(UserProfileRequest request) => _api.UpsertProfileAsync(request);
}
