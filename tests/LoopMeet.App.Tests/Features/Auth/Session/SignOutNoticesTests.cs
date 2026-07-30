using LoopMeet.App.Features.Auth.Session;

namespace LoopMeet.App.Tests.Features.Auth.Session;

public sealed class SignOutNoticesTests
{
    [Fact]
    public void UserInitiated_HasNoNotice()
    {
        Assert.Null(SignOutNotices.For(SignOutReason.UserInitiated));
    }

    [Fact]
    public void NullReason_HasNoNotice()
    {
        Assert.Null(SignOutNotices.For(null));
    }

    [Fact]
    public void SessionRejected_ExplainsTheSessionEnded()
    {
        Assert.Equal("Your session ended. Please sign in again.", SignOutNotices.For(SignOutReason.SessionRejected));
    }

    [Fact]
    public void SessionRejectedWithUnsavedInput_MentionsTheLostInput()
    {
        Assert.Equal(
            "Your session ended and unsaved changes were lost. Please sign in again.",
            SignOutNotices.For(SignOutReason.SessionRejectedWithUnsavedInput));
    }
}
