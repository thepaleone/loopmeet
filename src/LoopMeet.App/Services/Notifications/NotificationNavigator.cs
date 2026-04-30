namespace LoopMeet.App.Services.Notifications;

public sealed class NotificationNavigator
{
    public Task NavigateAsync(NotificationIntent intent)
    {
        var route = intent.NotificationType switch
        {
            "invitation.new" => "//Invitations/Pending",
            "meetup.created" or "meetup.updated" or "meetup.canceled" when !string.IsNullOrWhiteSpace(intent.TargetId)
                => $"//Groups/Detail?groupId={intent.TargetId}",
            "meetup.today_reminder" => "//Home",
            _ => intent.FallbackRoute == "pending_invitations" ? "//Invitations/Pending" : "//Home"
        };

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
    }
}
