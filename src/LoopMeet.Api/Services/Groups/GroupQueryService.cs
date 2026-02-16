using LoopMeet.Api.Contracts;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Api.Services.Groups;

public sealed class GroupQueryService
{
    private readonly LoopMeetDbContext _dbContext;

    public GroupQueryService(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GroupsResponse> GetGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var owned = await _dbContext.Groups
            .Where(group => group.OwnerUserId == userId)
            .OrderBy(group => group.Name)
            .Select(group => new GroupSummaryResponse
            {
                Id = group.Id,
                Name = group.Name,
                OwnerUserId = group.OwnerUserId
            })
            .ToListAsync(cancellationToken);

        var memberGroups = await _dbContext.Memberships
            .Where(membership => membership.UserId == userId)
            .Join(_dbContext.Groups,
                membership => membership.GroupId,
                group => group.Id,
                (_, group) => group)
            .Where(group => group.OwnerUserId != userId)
            .OrderBy(group => group.Name)
            .Select(group => new GroupSummaryResponse
            {
                Id = group.Id,
                Name = group.Name,
                OwnerUserId = group.OwnerUserId
            })
            .ToListAsync(cancellationToken);

        return new GroupsResponse
        {
            Owned = owned,
            Member = memberGroups
        };
    }

    public async Task<GroupDetailResponse?> GetGroupDetailAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups
            .Where(candidate => candidate.Id == groupId)
            .Select(candidate => new GroupSummaryResponse
            {
                Id = candidate.Id,
                Name = candidate.Name,
                OwnerUserId = candidate.OwnerUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        var members = await _dbContext.Memberships
            .Where(membership => membership.GroupId == groupId)
            .Join(_dbContext.Users,
                membership => membership.UserId,
                user => user.Id,
                (membership, user) => new GroupMemberResponse
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Role = membership.Role
                })
            .OrderBy(member => member.DisplayName)
            .ToListAsync(cancellationToken);

        return new GroupDetailResponse
        {
            Id = group.Id,
            Name = group.Name,
            OwnerUserId = group.OwnerUserId,
            Members = members
        };
    }
}
