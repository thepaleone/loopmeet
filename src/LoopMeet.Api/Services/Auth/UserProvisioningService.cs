using LoopMeet.Api.Contracts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Auth;

public sealed class UserProvisioningService
{
    private readonly IUserRepository _userRepository;

    public UserProvisioningService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> UpsertProfileAsync(Guid userId, UserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing is null)
        {
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

        existing.DisplayName = request.DisplayName;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);
        return existing;
    }
}
