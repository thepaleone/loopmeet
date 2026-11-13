using LoopMeet.App.Features.Profile;
using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App.Tests.Features.Profile;

public sealed class ProfileTextTests
{
    [Theory]
    [InlineData("Sarah Chen", "Sarah Chen", false)]
    [InlineData("Sarah Chen", "Sarah C.", true)]
    [InlineData("Sarah Chen", "", false)]
    [InlineData("Sarah Chen", "   ", false)]
    [InlineData("Sarah Chen", null, false)]
    [InlineData("Sarah Chen", "Sarah Chen ", false)]
    [InlineData("", "Sarah", true)]
    [InlineData("Sarah Chen", "sarah chen", true)]
    public void CanSave_OnlyForAChangedNonBlankName(string? saved, string? edited, bool expected)
    {
        Assert.Equal(expected, ProfileText.CanSave(saved, edited));
    }

    [Theory]
    [InlineData("2026-11-03T10:00:00+00:00", "Member since Nov 2026")]
    [InlineData("2027-01-31T23:00:00-08:00", "Member since Jan 2027")]
    [InlineData("2026-05-01T00:00:00+00:00", "Member since May 2026")]
    public void MemberSince_ShowsTheJoinMonthAndYear(string userSince, string expected)
    {
        // The value's own offset is used, as the old "User since" date did (data-model A1).
        Assert.Equal(expected, ProfileText.MemberSince(DateTimeOffset.Parse(userSince, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(0, "0 groups")]
    [InlineData(1, "1 group")]
    [InlineData(2, "2 groups")]
    [InlineData(5, "5 groups")]
    public void GroupCount_IsSingularOnlyForOne(int count, string expected)
    {
        Assert.Equal(expected, ProfileText.GroupCount(count));
    }

    [Theory]
    [InlineData(NotificationPermissionState.Granted, "On · Manage in device settings")]
    [InlineData(NotificationPermissionState.Denied, "Off · Turn on in device settings")]
    [InlineData(NotificationPermissionState.Unknown, "Off · Turn on in device settings")]
    public void NotificationStatus_IsOnOnlyWhenGranted(NotificationPermissionState state, string expected)
    {
        // Unknown (not yet asked, or unreadable) reads "Off": nothing is delivered either way.
        Assert.Equal(expected, ProfileText.NotificationStatus(state));
    }

    [Fact]
    public void DeleteAccountSubtitle_HasNoDuration()
    {
        // The grace period lives in SQL and AccountDeletionPolicy (016); this text must not become a third copy.
        Assert.Equal("Review what's deleted before you confirm", ProfileText.DeleteAccountSubtitle);
        Assert.DoesNotContain(ProfileText.DeleteAccountSubtitle, char.IsDigit);
    }
}
