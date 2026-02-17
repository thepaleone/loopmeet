using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class MembershipRepository : IMembershipRepository
{
    private readonly Client _client;

    public MembershipRepository(Client client)
    {
        _client = client;
    }

    public Task<Membership?> GetByUserAndGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return GetByUserAndGroupInternalAsync(userId, groupId);
    }

    public async Task<IReadOnlyList<Membership>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<MembershipRecord>()
            .Filter("group_id", Operator.Equals, groupId.ToString())
            .Get();

        return response.Models
            .Select(Map)
            .ToList();
    }

    public async Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        var record = Map(membership);
        await _client.From<MembershipRecord>().Insert(record);
    }

    private async Task<Membership?> GetByUserAndGroupInternalAsync(Guid userId, Guid groupId)
    {
        var response = await _client
            .From<MembershipRecord>()
            .Filter("user_id", Operator.Equals, userId.ToString())
            .Filter("group_id", Operator.Equals, groupId.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private static Membership Map(MembershipRecord record)
    {
        return new Membership
        {
            Id = record.Id,
            GroupId = record.GroupId,
            UserId = record.UserId,
            Role = record.Role,
            CreatedAt = record.CreatedAt
        };
    }

    private static MembershipRecord Map(Membership membership)
    {
        return new MembershipRecord
        {
            Id = membership.Id,
            GroupId = membership.GroupId,
            UserId = membership.UserId,
            Role = membership.Role,
            CreatedAt = membership.CreatedAt
        };
    }
}
