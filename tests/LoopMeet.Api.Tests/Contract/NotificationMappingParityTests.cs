namespace LoopMeet.Api.Tests.Contract;

public sealed class NotificationMappingParityTests
{
    [Fact]
    public void MappingRegistry_IncludesAllRequiredTypes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../supabase/functions/_shared/notification-mapping-registry.ts"));
        var source = File.ReadAllText(path);

        Assert.Contains("invitation.new", source, StringComparison.Ordinal);
        Assert.Contains("meetup.created", source, StringComparison.Ordinal);
        Assert.Contains("meetup.updated", source, StringComparison.Ordinal);
        Assert.Contains("meetup.canceled", source, StringComparison.Ordinal);
        Assert.Contains("meetup.today_reminder", source, StringComparison.Ordinal);
    }
}
