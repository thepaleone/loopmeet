namespace LoopMeet.App.Services.Notifications;

using LoopMeet.App.Features.Home.Models;

public sealed class NotificationRouteMap
{
    public string Resolve(NotificationIntent intent)
    {
        return intent.NotificationType switch
        {
            "invitation.new" => SignedInTabs.InvitationsShellPath,
            "meetup.created" or "meetup.updated" or "meetup.canceled" when !string.IsNullOrWhiteSpace(intent.TargetId)
                => $"{SignedInTabs.GroupsShellPath}/group-detail?groupId={intent.TargetId}",
            "meetup.today_reminder" => SignedInTabs.HomeShellPath,
            _ => intent.FallbackRoute == "pending_invitations" ? SignedInTabs.InvitationsShellPath : SignedInTabs.HomeShellPath
        };
    }
}
