using LoopMeet.Core.Models;
using LoopMeet.Core.Validators;
using Xunit;

namespace LoopMeet.Core.Tests.Validators;

public sealed class UserValidatorTests
{
    [Fact]
    public void FailsWhenRequiredFieldsMissing()
    {
        var validator = new UserValidator();
        var user = new User();

        var result = validator.Validate(user);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PassesWhenRequiredFieldsPresent()
    {
        var validator = new UserValidator();
        var user = new User
        {
            DisplayName = "LoopMeet User",
            Email = "user@example.com"
        };

        var result = validator.Validate(user);

        Assert.True(result.IsValid);
    }
}
