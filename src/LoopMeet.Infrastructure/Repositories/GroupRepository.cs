using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly Client _client;

    public GroupRepository(Client client)
    {
        _client = client;
    }

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(id);
    }

    public async Task<IReadOnlyList<Group>> ListByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Group>();
        }

        var response = await _client.From<GroupRecord>().Get();
        return response.Models
            .Where(group => ids.Contains(group.Id))
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<Group>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<GroupRecord>()
            .Filter("owner_user_id", Operator.Equals, ownerUserId.ToString())
            .Get();

        return response.Models
            .Select(Map)
            .OrderBy(group => group.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Group>> ListMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var membershipResponse = await _client
            .From<MembershipRecord>()
            .Filter("member_user_id", Operator.Equals, userId.ToString())
            .Get();

        var groupIds = membershipResponse.Models
            .Select(membership => membership.GroupId)
            .Distinct()
            .ToList();

        if (groupIds.Count == 0)
        {
            return Array.Empty<Group>();
        }

        var groupsResponse = await _client.From<GroupRecord>().Get();
        return groupsResponse.Models
            .Where(group => groupIds.Contains(group.Id))
            .Select(Map)
            .OrderBy(group => group.Name)
            .ToList();
    }

    public async Task<bool> ExistsNameForOwnerAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<GroupRecord>()
            .Filter("owner_user_id", Operator.Equals, ownerUserId.ToString())
            .Filter("name", Operator.Equals, name)
            .Get();

        return response.Models.Count > 0;
    }

    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        var record = Map(group);
        var newGroup = await _client.From<GroupRecord>().Insert(record);
        return Map(newGroup.Models.First());
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        var record = Map(group);
        await _client.From<GroupRecord>().Update(record);
    }

    private async Task<Group?> GetByIdInternalAsync(Guid id)
    {
        var response = await _client
            .From<GroupRecord>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private static Group Map(GroupRecord record)
    {
        return new Group
        {
            Id = record.Id,
            OwnerUserId = record.OwnerUserId,
            Name = record.Name,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static GroupRecord Map(Group group)
    {
        return new GroupRecord
        {
            Id = group.Id,
            OwnerUserId = group.OwnerUserId,
            Name = group.Name,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}
