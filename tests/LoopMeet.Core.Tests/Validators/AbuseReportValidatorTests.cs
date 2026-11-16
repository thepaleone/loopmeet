using LoopMeet.Core.Models;
using LoopMeet.Core.Validators;
using Xunit;

namespace LoopMeet.Core.Tests.Validators;

public sealed class AbuseReportValidatorTests
{
    private static readonly AbuseReportValidator Validator = new();

    private static NewAbuseReport Report(
        ReportReason reason = ReportReason.Spam,
        string details = "They keep posting ads.",
        Guid? reportedUserId = null) =>
        new(Guid.NewGuid(), reportedUserId, reason, details);

    [Theory]
    [InlineData(ReportReason.InappropriatePhotoOrName)]
    [InlineData(ReportReason.HarassmentOrThreats)]
    [InlineData(ReportReason.ChildSafety)]
    [InlineData(ReportReason.Spam)]
    [InlineData(ReportReason.Other)]
    public void AcceptsEveryDefinedReason(ReportReason reason)
    {
        Assert.True(Validator.Validate(Report(reason)).IsValid);
    }

    [Fact]
    public void RefusesAnUndefinedReason()
    {
        Assert.False(Validator.Validate(Report((ReportReason)99)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefusesBlankDetails(string details)
    {
        Assert.False(Validator.Validate(Report(details: details)).IsValid);
    }

    [Fact]
    public void CountsDetailsAfterTrimming()
    {
        Assert.True(Validator.Validate(Report(details: "  " + new string('x', 1000) + "  ")).IsValid);
        Assert.True(Validator.Validate(Report(details: "x")).IsValid);
        Assert.False(Validator.Validate(Report(details: new string('x', 1001))).IsValid);
    }

    [Fact]
    public void RefusesAnEmptyReportedUserButAllowsTheGroupItself()
    {
        Assert.False(Validator.Validate(Report(reportedUserId: Guid.Empty)).IsValid);
        Assert.True(Validator.Validate(Report(reportedUserId: null)).IsValid);
        Assert.True(Validator.Validate(Report(reportedUserId: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void RefusesAMissingGroup()
    {
        var report = Report() with { GroupId = Guid.Empty };

        Assert.False(Validator.Validate(report).IsValid);
    }

    [Theory]
    [InlineData(ReportReason.InappropriatePhotoOrName, "inappropriate_photo_or_name")]
    [InlineData(ReportReason.HarassmentOrThreats, "harassment_or_threats")]
    [InlineData(ReportReason.ChildSafety, "child_safety")]
    [InlineData(ReportReason.Spam, "spam")]
    [InlineData(ReportReason.Other, "other")]
    public void ReasonCodesMatchTheDatabase(ReportReason reason, string code)
    {
        Assert.Equal(code, ReportReasonCodes.ToCode(reason));
        Assert.True(ReportReasonCodes.TryParse(code, out var parsed));
        Assert.Equal(reason, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("rude")]
    [InlineData("Spam")]
    public void UnknownReasonCodesDoNotParse(string code)
    {
        Assert.False(ReportReasonCodes.TryParse(code, out _));
    }
}
