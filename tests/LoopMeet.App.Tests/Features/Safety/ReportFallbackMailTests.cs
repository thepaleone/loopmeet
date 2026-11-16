using LoopMeet.App.Features.Safety;

namespace LoopMeet.App.Tests.Features.Safety;

public sealed class ReportFallbackMailTests
{
    [Fact]
    public void AddressesTheSafetyInboxWithTheReasonInTheSubject()
    {
        var mail = ReportFallbackMail.Compose("Book club", "Ann", "Harassment or threats", "Threats.", "0.1.9 (9)");

        Assert.Equal("abuse@loopmeet.io", mail.To);
        Assert.Equal("LoopMeet report: Harassment or threats", mail.Subject);
    }

    [Fact]
    public void TheBodyHoldsWhatTheUserChoseAndTheAppVersion()
    {
        var mail = ReportFallbackMail.Compose("Book club", "Ann", "Spam", "  Posting ads every day.  ", "0.1.9 (9)");

        Assert.Equal(
            "Group: Book club\nReported: Ann\nReason: Spam\nApp version: 0.1.9 (9)\n\nDetails:\nPosting ads every day.",
            mail.Body);
    }

    [Fact]
    public void AReportAboutTheGroupItselfSaysSo()
    {
        var mail = ReportFallbackMail.Compose("Book club", null, "Other", "The name.", "0.1.9 (9)");

        Assert.Contains("Reported: The group itself\n", mail.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBodyHasNoIdentifiers()
    {
        var mail = ReportFallbackMail.Compose("Book club", "Ann", "Spam", "Ads.", "0.1.9 (9)");

        Assert.DoesNotMatch("[0-9a-f]{8}-[0-9a-f]{4}-", mail.Body);
    }
}
