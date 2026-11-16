using FluentValidation;
using LoopMeet.Core.Models;

namespace LoopMeet.Core.Validators;

/// <summary>Early feedback for a report. submit_abuse_report() enforces the same rules authoritatively.</summary>
public sealed class AbuseReportValidator : AbstractValidator<NewAbuseReport>
{
    public const int MaxDetailsLength = 1000;

    public AbuseReportValidator()
    {
        RuleFor(report => report.GroupId).NotEmpty();
        RuleFor(report => report.Reason).IsInEnum();
        RuleFor(report => report.Details)
            .Must(details => details.Trim().Length is >= 1 and <= MaxDetailsLength);
        RuleFor(report => report.ReportedUserId)
            .Must(id => id != Guid.Empty)
            .When(report => report.ReportedUserId.HasValue);
    }
}
