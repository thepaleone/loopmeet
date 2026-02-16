using FluentValidation;
using LoopMeet.Core.Models;

namespace LoopMeet.Core.Validators;

public sealed class GroupValidator : AbstractValidator<Group>
{
    public GroupValidator()
    {
        RuleFor(group => group.Name).NotEmpty();
        RuleFor(group => group.OwnerUserId).NotEmpty();
    }
}
