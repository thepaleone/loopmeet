using LoopMeet.App.Features.Safety;

namespace LoopMeet.App.Tests.Features.Safety;

public sealed class SafetyErrorTextTests
{
    [Theory]
    [InlineData("invalid_report", "Choose a reason and describe what happened (up to 1,000 characters).")]
    [InlineData("not_group_member", "You can only report groups you belong to.")]
    [InlineData("invalid_reported_user", "Choose someone in this group other than yourself.")]
    [InlineData("report_limit_reached", "You've sent 10 reports today. You can send more later, or email abuse@loopmeet.io.")]
    [InlineData("owner_cannot_leave", "Group owners can't leave their own group.")]
    public void EachCodeHasTheContractMessage(string code, string message)
    {
        Assert.Equal(message, SafetyErrorText.Message(code));
    }

    [Theory]
    [InlineData("not_group_member", true)]
    [InlineData("owner_cannot_leave", false)]
    [InlineData(null, false)]
    public void NotAMemberMeansTheLeaveAlreadyHappened(string? code, bool alreadyLeft)
    {
        Assert.Equal(alreadyLeft, SafetyErrorText.MeansAlreadyLeft(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("something_new")]
    public void AnUnknownCodeGetsAGeneralMessage(string? code)
    {
        Assert.Equal("Something went wrong. Please try again.", SafetyErrorText.Message(code));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    [InlineData(400, false)]
    [InlineData(403, false)]
    [InlineData(422, false)]
    [InlineData(429, false)]
    public void OnlyANetworkOrServerFailureOffersTheEmailRoute(int? status, bool offers)
    {
        Assert.Equal(offers, SafetyErrorText.OffersEmailFallback(status));
    }

    [Theory]
    [InlineData("{\"code\":\"report_limit_reached\",\"message\":\"x\"}", "report_limit_reached")]
    [InlineData("{\"message\":\"x\"}", null)]
    [InlineData("{\"code\":42}", null)]
    [InlineData("not json", null)]
    [InlineData("[]", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void CodeFromReadsTheErrorCodeOrNothing(string? body, string? expected)
    {
        Assert.Equal(expected, SafetyErrorText.CodeFrom(body));
    }
}
