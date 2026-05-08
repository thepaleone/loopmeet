namespace LoopMeet.App.Services.Notifications;

public static class NotificationTapBridge
{
    private static readonly object Sync = new();
    private static readonly Queue<IDictionary<string, object?>> Pending = new();

    public static void Publish(IDictionary<string, object?> additionalData)
    {
        lock (Sync)
        {
            Pending.Enqueue(additionalData);
        }
    }

    public static IReadOnlyList<IDictionary<string, object?>> Drain()
    {
        var buffered = new List<IDictionary<string, object?>>();

        lock (Sync)
        {
            while (Pending.Count > 0)
            {
                buffered.Add(Pending.Dequeue());
            }
        }

        return buffered;
    }
}
