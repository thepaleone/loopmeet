using LoopMeet.Core.Models;
using LoopMeet.Core.Validators;
using Xunit;

namespace LoopMeet.Core.Tests.Validators;

public sealed class GroupValidatorTests
{
    [Fact]
    public void FailsWhenMissingNameOrOwner()
    {
        var validator = new GroupValidator();
        var group = new Group();

        var result = validator.Validate(group);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PassesWhenRequiredFieldsPresent()
    {
        var validator = new GroupValidator();
        var group = new Group
        {
            Name = "Weekend Crew",
            OwnerUserId = Guid.NewGuid()
        };

        var result = validator.Validate(group);

        Assert.True(result.IsValid);
    }
}
