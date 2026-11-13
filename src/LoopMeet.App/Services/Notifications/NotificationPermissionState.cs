namespace LoopMeet.App.Services.Notifications;

/// <summary>
/// The app's view of the OS notification permission. Kept in its own MAUI-free file so the
/// Profile text rules (feature 022) can be unit tested against it.
/// </summary>
public enum NotificationPermissionState
{
    Unknown,
    Granted,
    Denied,
}
