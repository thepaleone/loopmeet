using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly Client _client;

    public InvitationRepository(Client client)
    {
        _client = client;
    }

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(id);
    }

    public async Task<IReadOnlyList<Invitation>> ListPendingByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<InvitationRecord>()
            .Filter("invited_email", Operator.Equals, email)
            .Filter("status", Operator.Equals, "pending")
            .Get();

        return response.Models
            .Select(Map)
            .OrderBy(invitation => invitation.CreatedAt)
            .ToList();
    }

    public async Task<bool> ExistsPendingForEmailAsync(Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<InvitationRecord>()
            .Filter("group_id", Operator.Equals, groupId.ToString())
            .Filter("invited_email", Operator.Equals, email)
            .Filter("status", Operator.Equals, "pending")
            .Get();

        return response.Models.Count > 0;
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        var record = Map(invitation);
        await _client.From<InvitationRecord>().Insert(record);
    }

    public async Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        var record = Map(invitation);
        await _client.From<InvitationRecord>().Update(record);
    }

    private async Task<Invitation?> GetByIdInternalAsync(Guid id)
    {
        var response = await _client
            .From<InvitationRecord>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private static Invitation Map(InvitationRecord record)
    {
        return new Invitation
        {
            Id = record.Id,
            GroupId = record.GroupId,
            InvitedEmail = record.InvitedEmail,
            InvitedUserId = record.InvitedUserId,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            AcceptedAt = record.AcceptedAt
        };
    }

    private static InvitationRecord Map(Invitation invitation)
    {
        return new InvitationRecord
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            InvitedEmail = invitation.InvitedEmail,
            InvitedUserId = invitation.InvitedUserId,
            Status = invitation.Status,
            CreatedAt = invitation.CreatedAt,
            AcceptedAt = invitation.AcceptedAt
        };
    }
}
