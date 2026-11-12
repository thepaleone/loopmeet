using LoopMeet.App.Features.Account;

namespace LoopMeet.App.Tests.Features.Account;

public sealed class AccountDeletionConfirmationTests
{
    [Fact]
    public void ExactDeleteConfirms()
    {
        Assert.True(AccountDeletionConfirmation.IsConfirmed("DELETE"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("delete")]
    [InlineData("Delete")]
    [InlineData(" DELETE")]
    [InlineData("DELETE ")]
    [InlineData("DELETED")]
    public void AnythingElseDoesNotConfirm(string? text)
    {
        Assert.False(AccountDeletionConfirmation.IsConfirmed(text));
    }
}
