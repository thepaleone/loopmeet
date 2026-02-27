using LoopMeet.Api.Contracts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Auth;

public sealed class UserProvisioningService
{
    private readonly IUserRepository _userRepository;
    private readonly ProfileAvatarResolver _avatarResolver;
    private readonly PasswordPolicyValidator _passwordPolicyValidator;
    private readonly IPasswordChangeService _passwordChangeService;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(IUserRepository userRepository,
        ProfileAvatarResolver avatarResolver,
        PasswordPolicyValidator passwordPolicyValidator,
        IPasswordChangeService passwordChangeService,
        ILogger<UserProvisioningService> logger)
    {
        _userRepository = userRepository;
        _avatarResolver = avatarResolver;
        _passwordPolicyValidator = passwordPolicyValidator;
        _passwordChangeService = passwordChangeService;
        _logger = logger;
    }

    public async Task<User> UpsertProfileAsync(Guid userId, UserProfileRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upserting profile for user {UserId}", userId);
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing is null)
        {
            _logger.LogInformation("Creating new profile for user {UserId}", userId);
            var user = new User
            {
                Id = userId,
                DisplayName = request.DisplayName,
                Email = request.Email ?? string.Empty,
                Phone = request.Phone,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _avatarResolver.ApplyFromRequest(user, request.SocialAvatarUrl, request.AvatarOverrideUrl);

            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }

        _logger.LogInformation("Updating existing profile for user {UserId}", userId);
        existing.DisplayName = request.DisplayName;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            existing.Email = request.Email;
        }

        existing.Phone = request.Phone;
        _avatarResolver.ApplyFromRequest(existing, request.SocialAvatarUrl, request.AvatarOverrideUrl);
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);
        return existing;
    }

    public async Task<User?> UpdateProfileAsync(Guid userId, UserProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.DisplayName = request.DisplayName;
        existing.Phone = request.Phone;
        _avatarResolver.ApplyFromRequest(existing, request.SocialAvatarUrl, request.AvatarOverrideUrl);
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);
        return existing;
    }

    public async Task<User?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading profile for user {UserId}", userId);
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        string? fallbackEmail,
        string? accessToken,
        PasswordChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasEmailFromRequest = !string.IsNullOrWhiteSpace(request.Email);

        if (string.IsNullOrWhiteSpace(request.NewPassword)
            || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            _logger.LogWarning(
                "Password change failed for {UserId}. Reason: {ReasonCode}",
                userId,
                PasswordChangeFailureReason.MissingFields);
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.MissingFields,
                "New password and confirmation are required.",
                hasEmailFromRequest: hasEmailFromRequest);
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Password change failed for {UserId}. Reason: {ReasonCode}",
                userId,
                PasswordChangeFailureReason.PasswordMismatch);
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.PasswordMismatch,
                "The new password and confirmation do not match.",
                hasEmailFromRequest: hasEmailFromRequest);
        }

        if (!_passwordPolicyValidator.TryValidate(request.NewPassword, out var error))
        {
            _logger.LogWarning(
                "Password change failed for {UserId}. Reason: {ReasonCode}",
                userId,
                PasswordChangeFailureReason.PasswordPolicyFailed);
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.PasswordPolicyFailed,
                error,
                hasEmailFromRequest: hasEmailFromRequest);
        }

        var profile = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var profileEmail = profile?.Email;
        var result = await _passwordChangeService.ChangePasswordAsync(
            userId,
            request.Email,
            profileEmail,
            fallbackEmail,
            accessToken,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Password change succeeded for {UserId}. HasEmailIdentity: {HasEmailIdentity}",
                userId,
                result.HasEmailIdentity);
        }
        else
        {
            _logger.LogWarning(
                "Password change failed for {UserId}. Reason: {ReasonCode}. HasEmailFromRequest: {HasEmailFromRequest}. HasEmailFromProfile: {HasEmailFromProfile}. HasEmailFromClaim: {HasEmailFromClaim}. HasEmailFromAuthUser: {HasEmailFromAuthUser}. HasEmailIdentity: {HasEmailIdentity}",
                userId,
                result.ReasonCode,
                result.HasEmailFromRequest,
                result.HasEmailFromProfile,
                result.HasEmailFromClaim,
                result.HasEmailFromAuthUser,
                result.HasEmailIdentity);
        }

        return result;
    }
}

public enum PasswordChangeFailureReason
{
    None,
    MissingFields,
    PasswordMismatch,
    PasswordPolicyFailed,
    MissingEmail,
    IdentityLookupFailed,
    CurrentPasswordInvalid,
    SupabaseUpdateFailed,
    SupabaseUnexpectedError,
    ServiceNotConfigured
}

public sealed record PasswordChangeResult(
    bool IsSuccess,
    PasswordChangeFailureReason ReasonCode,
    string? Error,
    bool HasEmailFromRequest = false,
    bool HasEmailFromProfile = false,
    bool HasEmailFromClaim = false,
    bool HasEmailFromAuthUser = false,
    bool HasEmailIdentity = false)
{
    public bool IsCurrentPasswordInvalid => ReasonCode == PasswordChangeFailureReason.CurrentPasswordInvalid;

    public static PasswordChangeResult Success(
        bool hasEmailFromRequest = false,
        bool hasEmailFromProfile = false,
        bool hasEmailFromClaim = false,
        bool hasEmailFromAuthUser = false,
        bool hasEmailIdentity = false) => new(
            true,
            PasswordChangeFailureReason.None,
            null,
            hasEmailFromRequest,
            hasEmailFromProfile,
            hasEmailFromClaim,
            hasEmailFromAuthUser,
            hasEmailIdentity);

    public static PasswordChangeResult Failure(
        PasswordChangeFailureReason reasonCode,
        string error,
        bool hasEmailFromRequest = false,
        bool hasEmailFromProfile = false,
        bool hasEmailFromClaim = false,
        bool hasEmailFromAuthUser = false,
        bool hasEmailIdentity = false) => new(
            false,
            reasonCode,
            error,
            hasEmailFromRequest,
            hasEmailFromProfile,
            hasEmailFromClaim,
            hasEmailFromAuthUser,
            hasEmailIdentity);
}
