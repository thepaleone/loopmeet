using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Supabase.Postgrest.Exceptions;
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

    public async Task<IReadOnlyList<Membership>> ListMembersByGroupsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return Array.Empty<Membership>();
        }

        var response = await _client
            .From<MembershipRecord>()
            .Filter("group_id", Operator.In, groupIds.Select(id => id.ToString()).ToList())
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

    public async Task AddFromInvitationAsync(Membership membership, string invitedEmail, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["p_id"] = membership.Id,
            ["p_group_id"] = membership.GroupId,
            ["p_member_user_id"] = membership.UserId,
            ["p_role"] = membership.Role,
            ["p_email"] = invitedEmail
        };
        await _client.Rpc("create_membership_from_invitation", parameters);
    }

    public async Task<MembershipExitResult> LeaveAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Rpc("leave_group", new Dictionary<string, object> { ["p_group_id"] = groupId });
            return MembershipExitResult.Done;
        }
        catch (PostgrestException exception) when (exception.Message.Contains("not_group_member", StringComparison.Ordinal))
        {
            return MembershipExitResult.NotGroupMember;
        }
        catch (PostgrestException exception) when (exception.Message.Contains("owner_cannot_leave", StringComparison.Ordinal))
        {
            return MembershipExitResult.OwnerCannotLeave;
        }
    }

    private async Task<Membership?> GetByUserAndGroupInternalAsync(Guid userId, Guid groupId)
    {
        var response = await _client
            .From<MembershipRecord>()
            .Filter("member_user_id", Operator.Equals, userId.ToString())
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
