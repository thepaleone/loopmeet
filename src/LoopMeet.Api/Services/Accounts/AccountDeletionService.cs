using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Accounts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Accounts;

public enum AccountDeletionCommandStatus
{
    Success,
    InvalidConfirmation,
    DeletionInProgress
}

public sealed record AccountDeletionCommandResult(AccountDeletionCommandStatus Status, AccountDeletion? Deletion);

public sealed class AccountDeletionService
{
    public const string Confirmation = "DELETE";

    private readonly IAccountDeletionRepository _deletionRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICacheService _cacheService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(
        IAccountDeletionRepository deletionRepository,
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        ICacheService cacheService,
        TimeProvider timeProvider,
        ILogger<AccountDeletionService> logger)
    {
        _deletionRepository = deletionRepository;
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _cacheService = cacheService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public static string CacheKey(Guid userId) => $"account-deletion:{userId}";

    /// <summary>
    /// The status clients see. Once the deletion date has passed the account counts as purging,
    /// whether or not the hourly purge job has picked it up yet, because restore is no longer possible.
    /// </summary>
    public static AccountDeletionStatus? EffectiveStatus(AccountDeletion? deletion, DateTimeOffset now)
    {
        if (deletion is null)
        {
            return null;
        }

        return deletion.Status == AccountDeletionStatus.Purging || deletion.DeletesAt <= now
            ? AccountDeletionStatus.Purging
            : AccountDeletionStatus.Pending;
    }

    public async Task<AccountDeletionStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var deletion = await _deletionRepository.GetAsync(userId, cancellationToken);
        return ToResponse(deletion);
    }

    public async Task<AccountDeletionPreviewResponse> GetPreviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var deletion = await _deletionRepository.GetAsync(userId, cancellationToken);
        var ownedGroups = await _groupRepository.ListOwnedAsync(userId, cancellationToken);
        var memberships = await _membershipRepository.ListMembersByGroupsAsync(
            ownedGroups.Select(group => group.Id).ToList(),
            cancellationToken);
        var memberCounts = memberships
            .GroupBy(membership => membership.GroupId)
            .ToDictionary(group => group.Key, group => group.Count());

        return new AccountDeletionPreviewResponse
        {
            DeletesAt = deletion?.DeletesAt ?? _timeProvider.GetUtcNow() + AccountDeletionPolicy.GracePeriod,
            OwnedGroups = ownedGroups
                .OrderBy(group => group.Name, StringComparer.Ordinal)
                .Select(group => new OwnedGroupSummary
                {
                    Id = group.Id,
                    Name = group.Name,
                    MemberCount = memberCounts.GetValueOrDefault(group.Id)
                })
                .ToList()
        };
    }

    public async Task<AccountDeletionCommandResult> RequestAsync(Guid userId, string? confirmation, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(confirmation, Confirmation, StringComparison.Ordinal))
        {
            _logger.LogWarning("AccountDeletionInvalidConfirmation for {UserId}", userId);
            return new AccountDeletionCommandResult(AccountDeletionCommandStatus.InvalidConfirmation, null);
        }

        var existing = await _deletionRepository.GetAsync(userId, cancellationToken);
        if (EffectiveStatus(existing, _timeProvider.GetUtcNow()) == AccountDeletionStatus.Purging)
        {
            _logger.LogWarning("AccountDeletionRequestRefused for {UserId}: deletion in progress", userId);
            return new AccountDeletionCommandResult(AccountDeletionCommandStatus.DeletionInProgress, existing);
        }

        var deletion = await _deletionRepository.RequestAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKey(userId));

        if (existing is null)
        {
            _logger.LogInformation("AccountDeletionRequested for {UserId}, deletes at {DeletesAt}", userId, deletion.DeletesAt);
        }
        else
        {
            _logger.LogInformation("AccountDeletionAlreadyPending for {UserId}, deletes at {DeletesAt}", userId, deletion.DeletesAt);
        }

        return new AccountDeletionCommandResult(AccountDeletionCommandStatus.Success, deletion);
    }

    public AccountDeletionStatusResponse ToResponse(AccountDeletion? deletion)
    {
        return EffectiveStatus(deletion, _timeProvider.GetUtcNow()) switch
        {
            null => new AccountDeletionStatusResponse { Status = AccountDeletionStatusNames.None },
            var status => new AccountDeletionStatusResponse
            {
                Status = status == AccountDeletionStatus.Purging ? AccountDeletionStatusNames.Purging : AccountDeletionStatusNames.Pending,
                RequestedAt = deletion!.RequestedAt,
                DeletesAt = deletion.DeletesAt
            }
        };
    }
}
