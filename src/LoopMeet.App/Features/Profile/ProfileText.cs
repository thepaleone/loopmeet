using System.Globalization;
using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App.Features.Profile;

/// <summary>
/// The Profile page's display rules (feature 022). MAUI-free so it can be unit tested:
/// keep Microsoft.Maui.* out of this file.
/// </summary>
public static class ProfileText
{
    /// <summary>
    /// No day count: the grace period is defined in SQL (request_account_deletion) and in
    /// AccountDeletionPolicy (016), and this text must not become a third copy.
    /// </summary>
    public const string DeleteAccountSubtitle = "Review what's deleted before you confirm";

    /// <summary>
    /// Save is offered only for a real change: a name that isn't blank and differs from the saved one.
    /// Surrounding spaces don't count as a change.
    /// </summary>
    public static bool CanSave(string? savedName, string? editedName)
    {
        var edited = editedName?.Trim();
        return !string.IsNullOrEmpty(edited) && !string.Equals(edited, savedName, StringComparison.Ordinal);
    }

    /// <summary>"Member since Nov 2026", in the offset the Api returned the date in (data-model A1).</summary>
    public static string MemberSince(DateTimeOffset userSince) =>
        "Member since " + userSince.ToString("MMM yyyy", CultureInfo.InvariantCulture);

    public static string GroupCount(int count) => count == 1 ? "1 group" : $"{count} groups";

    /// <summary>Notifications are on only when granted; not yet asked and unreadable both read "Off".</summary>
    public static string NotificationStatus(NotificationPermissionState state) => state switch
    {
        NotificationPermissionState.Granted => "On · Manage in device settings",
        _ => "Off · Turn on in device settings",
    };
}
