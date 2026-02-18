using Microsoft.Extensions.Options;

namespace LoopMeet.Api.Services.Auth;

public sealed class PasswordPolicyValidator
{
    private readonly PasswordPolicyOptions _options;

    public PasswordPolicyValidator(IOptions<PasswordPolicyOptions> options)
    {
        _options = options.Value;
    }

    public bool TryValidate(string password, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Password is required.";
            return false;
        }

        if (password.Length < _options.MinLength)
        {
            error = $"Password must be at least {_options.MinLength} characters.";
            return false;
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            error = "Password must include a lowercase letter.";
            return false;
        }

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            error = "Password must include an uppercase letter.";
            return false;
        }

        if (_options.RequireNumber && !password.Any(char.IsDigit))
        {
            error = "Password must include a number.";
            return false;
        }

        if (_options.RequireSymbol && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            error = "Password must include a symbol.";
            return false;
        }

        return true;
    }
}
