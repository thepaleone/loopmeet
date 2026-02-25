using FluentValidation;
using LoopMeet.Core.Models;

namespace LoopMeet.Core.Validators;

public sealed class InvitationValidator : AbstractValidator<Invitation>
{
    public InvitationValidator()
    {
        RuleFor(invitation => invitation.InvitedEmail).NotEmpty().EmailAddress();
        RuleFor(invitation => invitation.GroupId).NotEmpty();
        RuleFor(invitation => invitation.Status).NotEmpty();
    }
}
