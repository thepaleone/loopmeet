using LoopMeet.Api.Contracts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Auth;

public sealed class UserProvisioningService
{
    private readonly IUserRepository _userRepository;

    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(IUserRepository userRepository,
        ILogger<UserProvisioningService> logger)
    {
        _userRepository = userRepository;
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
                Email = request.Email,
                Phone = request.Phone,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }

        _logger.LogInformation("Updating existing profile for user {UserId}", userId);
        existing.DisplayName = request.DisplayName;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);
        return existing;
    }
}
