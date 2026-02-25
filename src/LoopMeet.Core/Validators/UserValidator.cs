using FluentValidation;
using LoopMeet.Core.Models;

namespace LoopMeet.Core.Validators;

public sealed class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(user => user.DisplayName).NotEmpty();
        RuleFor(user => user.Email).NotEmpty().EmailAddress();
    }
}
