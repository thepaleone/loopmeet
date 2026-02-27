using LoopMeet.Api.Services.Auth;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryPasswordChangeService : IPasswordChangeService
{
    private readonly InMemoryStore _store;

    public InMemoryPasswordChangeService(InMemoryStore store)
    {
        _store = store;
    }

    public Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        string? requestedEmail,
        string? profileEmail,
        string? claimEmail,
        string? accessToken,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var hasEmailFromRequest = !string.IsNullOrWhiteSpace(requestedEmail);
            var hasEmailFromProfile = !string.IsNullOrWhiteSpace(profileEmail);
            var hasEmailFromClaim = !string.IsNullOrWhiteSpace(claimEmail);

            var hasEmailProvider = _store.AuthIdentities.Any(identity =>
                identity.UserId == userId
                && string.Equals(identity.Provider, "email", StringComparison.OrdinalIgnoreCase));

            var resolvedEmail = hasEmailFromRequest
                ? requestedEmail
                : hasEmailFromProfile
                    ? profileEmail
                    : claimEmail;

            if (string.IsNullOrWhiteSpace(resolvedEmail))
            {
                var missingEmailMessage = hasEmailProvider
                    ? "Enter your account email to verify your current password."
                    : "Enter your account email to set your password.";
                return Task.FromResult(PasswordChangeResult.Failure(
                    PasswordChangeFailureReason.MissingEmail,
                    missingEmailMessage,
                    hasEmailFromRequest,
                    hasEmailFromProfile,
                    hasEmailFromClaim,
                    hasEmailIdentity: hasEmailProvider));
            }

            if (hasEmailProvider && !string.Equals(currentPassword, "CurrentPass123!", StringComparison.Ordinal))
            {
                return Task.FromResult(PasswordChangeResult.Failure(
                    PasswordChangeFailureReason.CurrentPasswordInvalid,
                    "Current password is incorrect.",
                    hasEmailFromRequest,
                    hasEmailFromProfile,
                    hasEmailFromClaim,
                    hasEmailIdentity: true));
            }

            return Task.FromResult(PasswordChangeResult.Success(
                hasEmailFromRequest,
                hasEmailFromProfile,
                hasEmailFromClaim,
                hasEmailIdentity: hasEmailProvider));
        }
    }
}
