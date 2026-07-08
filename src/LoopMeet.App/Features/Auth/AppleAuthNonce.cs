using System.Security.Cryptography;
using System.Text;

namespace LoopMeet.App.Features.Auth;

internal static class AppleAuthNonce
{
    public static (string Raw, string Hashed) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hashed = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (raw, hashed);
    }
}
