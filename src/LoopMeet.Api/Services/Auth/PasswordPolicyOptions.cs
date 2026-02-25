namespace LoopMeet.Api.Services.Auth;

public sealed class PasswordPolicyOptions
{
    public int MinLength { get; init; } = 8;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireNumber { get; init; } = true;
    public bool RequireSymbol { get; init; } = true;
}
