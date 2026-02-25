using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Groups;

public sealed class GroupQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GroupQueryService> _logger;

    public GroupQueryService(
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        ILogger<GroupQueryService> logger)
    {
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GroupsResponse> GetGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"groups:{userId}";
        _logger.LogInformation("Loading groups for {UserId}", userId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var ownedGroups = await _groupRepository.ListOwnedAsync(userId, cancellationToken);
            var memberGroups = await _groupRepository.ListMemberAsync(userId, cancellationToken);

            var owned = ownedGroups
                .Select(MapSummary)
                .OrderBy(group => group.Name)
                .ToList();

            var member = memberGroups
                .Where(group => group.OwnerUserId != userId)
                .Select(MapSummary)
                .OrderBy(group => group.Name)
                .ToList();

            _logger.LogInformation("Loaded groups for {UserId} owned={OwnedCount} member={MemberCount}",
                userId,
                owned.Count,
                member.Count);
            return new GroupsResponse
            {
                Owned = owned,
                Member = member
            };
        }) ?? new GroupsResponse();
    }

    public async Task<GroupDetailResponse?> GetGroupDetailAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"group-detail:{groupId}";
        _logger.LogInformation("Loading group detail {GroupId}", groupId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            if (group is null)
            {
                _logger.LogWarning("Group detail not found {GroupId}", groupId);
                return null;
            }

            var memberships = await _membershipRepository.ListMembersAsync(groupId, cancellationToken);
            var userIds = memberships.Select(member => member.UserId).Distinct().ToList();
            var users = await _userRepository.ListByIdsAsync(userIds, cancellationToken);
            var userLookup = users.ToDictionary(user => user.Id, user => user.DisplayName);

            var members = memberships
                .Select(member => new GroupMemberResponse
                {
                    UserId = member.UserId,
                    DisplayName = userLookup.TryGetValue(member.UserId, out var name) ? name : "",
                    Role = member.Role
                })
                .OrderBy(member => member.DisplayName)
                .ToList();

            _logger.LogInformation("Loaded group detail {GroupId} members={MemberCount}", groupId, members.Count);
            return new GroupDetailResponse
            {
                Id = group.Id,
                Name = group.Name,
                OwnerUserId = group.OwnerUserId,
                Members = members
            };
        });
    }

    private static GroupSummaryResponse MapSummary(Group group)
    {
        return new GroupSummaryResponse
        {
            Id = group.Id,
            Name = group.Name,
            OwnerUserId = group.OwnerUserId
        };
    }
}
