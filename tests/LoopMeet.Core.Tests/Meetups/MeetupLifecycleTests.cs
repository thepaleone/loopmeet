using System.Text.Json;
using LoopMeet.Core.Meetups;
using Xunit;

namespace LoopMeet.Core.Tests.Meetups;

/// <summary>
/// Drives <see cref="MeetupLifecycle.IsPast"/> from the case file the Deno
/// edge-function tests also read, so the C# and TypeScript rules cannot drift.
/// </summary>
public sealed class MeetupLifecycleTests
{
    private static readonly string CasesPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../supabase/functions/_shared/meetup-lifecycle.cases.json"));

    public static IEnumerable<object?[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CasesPath));
        foreach (var item in document.RootElement.EnumerateArray())
        {
            yield return new object?[]
            {
                item.GetProperty("name").GetString(),
                item.GetProperty("scheduledAt").GetString(),
                item.GetProperty("timezone").GetString(),
                item.GetProperty("now").GetString(),
                item.GetProperty("expectedIsPast").GetBoolean()
            };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IsPast_MatchesSharedCase(string name, string scheduledAt, string? timezone, string now, bool expectedIsPast)
    {
        var actual = MeetupLifecycle.IsPast(
            DateTimeOffset.Parse(scheduledAt),
            timezone,
            DateTimeOffset.Parse(now));

        Assert.True(expectedIsPast == actual, $"{name}: expected {expectedIsPast}, got {actual}");
    }

    [Fact]
    public void SharedCaseFile_IsNotEmptied()
    {
        Assert.True(Cases().Count() >= 15, "meetup-lifecycle.cases.json must keep at least 15 cases.");
    }
}
