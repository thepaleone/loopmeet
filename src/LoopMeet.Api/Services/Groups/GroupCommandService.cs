using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Groups;

public enum GroupCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    DuplicateName,
    InvalidName
}

public sealed record GroupCommandResult(GroupCommandStatus Status, GroupSummaryResponse? Group);

public sealed class GroupCommandService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GroupCommandService> _logger;

    public GroupCommandService(
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        ICacheService cacheService,
        ILogger<GroupCommandService> logger)
    {
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GroupCommandResult> CreateAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            _logger.LogWarning("Create group invalid name for {UserId}", ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.InvalidName, null);
        }

        if (await _groupRepository.ExistsNameForOwnerAsync(ownerUserId, trimmedName, cancellationToken))
        {
            _logger.LogWarning("Create group duplicate name for {UserId}", ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.DuplicateName, null);
        }

        _logger.LogInformation("Creating group for {UserId}", ownerUserId);
        var now = DateTimeOffset.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = trimmedName,
            CreatedAt = now,
            UpdatedAt = now
        };

        var newGroup = await _groupRepository.AddAsync(group, cancellationToken);

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            GroupId = newGroup.Id,
            UserId = ownerUserId,
            Role = "owner",
            CreatedAt = now
        };

        await _membershipRepository.AddAsync(membership, cancellationToken);

        await _cacheService.RemoveAsync($"groups:{ownerUserId}");
        _logger.LogInformation("Created group {GroupId} for {UserId}", newGroup.Id, ownerUserId);

        return new GroupCommandResult(GroupCommandStatus.Success, new GroupSummaryResponse
        {
            Id = newGroup.Id,
            Name = newGroup.Name,
            OwnerUserId = newGroup.OwnerUserId,
            MemberCount = 1
        });
    }

    public async Task<GroupCommandResult> RenameAsync(Guid groupId, Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            _logger.LogWarning("Rename group invalid name {GroupId} by {UserId}", groupId, ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.InvalidName, null);
        }

        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            _logger.LogWarning("Rename group not found {GroupId} by {UserId}", groupId, ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.NotFound, null);
        }

        if (group.OwnerUserId != ownerUserId)
        {
            _logger.LogWarning("Rename group forbidden {GroupId} by {UserId}", groupId, ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.Forbidden, null);
        }

        if (!string.Equals(group.Name, trimmedName, StringComparison.Ordinal)
            && await _groupRepository.ExistsNameForOwnerAsync(ownerUserId, trimmedName, cancellationToken))
        {
            _logger.LogWarning("Rename group duplicate name {GroupId} by {UserId}", groupId, ownerUserId);
            return new GroupCommandResult(GroupCommandStatus.DuplicateName, null);
        }

        if (!string.Equals(group.Name, trimmedName, StringComparison.Ordinal))
        {
            group.Name = trimmedName;
            group.UpdatedAt = DateTimeOffset.UtcNow;
            await _groupRepository.UpdateAsync(group, cancellationToken);

            await _cacheService.RemoveAsync($"groups:{ownerUserId}");
            await _cacheService.RemoveAsync($"group-detail:{groupId}");
            _logger.LogInformation("Renamed group {GroupId} by {UserId}", groupId, ownerUserId);
        }
        else
        {
            _logger.LogInformation("Rename group no-op {GroupId} by {UserId}", groupId, ownerUserId);
        }

        var memberCount = (await _membershipRepository.ListMembersAsync(groupId, cancellationToken)).Count;

        return new GroupCommandResult(GroupCommandStatus.Success, new GroupSummaryResponse
        {
            Id = group.Id,
            Name = group.Name,
            OwnerUserId = group.OwnerUserId,
            MemberCount = memberCount
        });
    }
}
