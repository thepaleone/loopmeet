using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App.Services.Auth;

public sealed class AuthSessionService
{
    private readonly NotificationPermissionService _permissionService;

    public AuthSessionService(NotificationPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task HandleSuccessfulSignInAsync()
    {
        if (_permissionService.ShouldPromptAfterSignIn())
        {
            await _permissionService.SetStateAsync(NotificationPermissionState.Unknown);
        }
    }
}
