using AuthModels = LoopMeet.App.Features.Auth.Models;
using ProfileModels = LoopMeet.App.Features.Profile.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface IUsersApi
{
    [Get("/users/profile")]
    Task<AuthModels.UserProfileResponse> GetProfileAsync();

    [Post("/users/profile")]
    Task<AuthModels.UserProfileResponse> UpsertProfileAsync([Body] AuthModels.UserProfileRequest request);

    [Patch("/users/profile")]
    Task<AuthModels.UserProfileResponse> UpdateProfileAsync([Body] ProfileModels.UserProfileUpdateRequest request);

    [Post("/users/password")]
    Task ChangePasswordAsync([Body] ProfileModels.PasswordChangeRequest request);
}

public sealed class UsersApi
{
    private readonly IUsersApi _api;

    public UsersApi(IUsersApi api)
    {
        _api = api;
    }

    public Task<AuthModels.UserProfileResponse> GetProfileAsync() => _api.GetProfileAsync();

    public Task<AuthModels.UserProfileResponse> UpsertProfileAsync(AuthModels.UserProfileRequest request) =>
        _api.UpsertProfileAsync(request);

    public async Task<ProfileModels.UserProfileResponse> GetProfileSummaryAsync() =>
        Map(await _api.GetProfileAsync());

    public async Task<ProfileModels.UserProfileResponse> UpdateProfileAsync(ProfileModels.UserProfileUpdateRequest request) =>
        Map(await _api.UpdateProfileAsync(request));

    public Task ChangePasswordAsync(ProfileModels.PasswordChangeRequest request) => _api.ChangePasswordAsync(request);

    private static ProfileModels.UserProfileResponse Map(AuthModels.UserProfileResponse response)
    {
        return new ProfileModels.UserProfileResponse
        {
            DisplayName = response.DisplayName,
            Email = response.Email,
            Phone = response.Phone,
            AvatarUrl = response.AvatarUrl,
            AvatarSource = response.AvatarSource,
            UserSince = response.UserSince,
            GroupCount = response.GroupCount,
            CanChangePassword = response.CanChangePassword,
            HasEmailProvider = response.HasEmailProvider,
            RequiresCurrentPassword = response.RequiresCurrentPassword,
            RequiresEmailForPasswordSetup = response.RequiresEmailForPasswordSetup
        };
    }
}
