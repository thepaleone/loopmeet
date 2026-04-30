namespace LoopMeet.App.Services.Notifications;

public sealed class NotificationNavigator
{
    private readonly NotificationRouteMap _routeMap;

    public NotificationNavigator(NotificationRouteMap routeMap)
    {
        _routeMap = routeMap;
    }

    public Task NavigateAsync(NotificationIntent intent)
    {
        var route = _routeMap.Resolve(intent);

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
    }
}
