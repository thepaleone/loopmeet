using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Invitations;

public enum InvitationCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    Duplicate,
    AlreadyMember,
    InvalidEmail
}

public sealed record InvitationCommandResult(InvitationCommandStatus Status, InvitationResponse? Invitation);

public sealed class InvitationCommandService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;

    public InvitationCommandService(
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository invitationRepository,
        IUserRepository userRepository,
        ICacheService cacheService)
    {
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
    }

    public async Task<InvitationCommandResult> CreateAsync(Guid ownerUserId, Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        var trimmedEmail = email.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            return new InvitationCommandResult(InvitationCommandStatus.InvalidEmail, null);
        }

        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (group.OwnerUserId != ownerUserId)
        {
            return new InvitationCommandResult(InvitationCommandStatus.Forbidden, null);
        }

        var existingUser = await _userRepository.GetByEmailAsync(trimmedEmail, cancellationToken);
        if (existingUser is not null)
        {
            var membership = await _membershipRepository.GetByUserAndGroupAsync(existingUser.Id, groupId, cancellationToken);
            if (membership is not null)
            {
                return new InvitationCommandResult(InvitationCommandStatus.AlreadyMember, null);
            }
        }

        if (await _invitationRepository.ExistsPendingForEmailAsync(groupId, trimmedEmail, cancellationToken))
        {
            return new InvitationCommandResult(InvitationCommandStatus.Duplicate, null);
        }

        var now = DateTimeOffset.UtcNow;
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            InvitedEmail = trimmedEmail,
            InvitedUserId = existingUser?.Id,
            Status = "pending",
            CreatedAt = now
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        await _cacheService.RemoveAsync($"pending-invitations:{trimmedEmail}");

        return new InvitationCommandResult(InvitationCommandStatus.Success, new InvitationResponse
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            InvitedEmail = invitation.InvitedEmail,
            Status = invitation.Status
        });
    }

    public async Task<InvitationCommandResult> AcceptAsync(Guid userId, string email, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.Status != "pending")
        {
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (!string.Equals(invitation.InvitedEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        var membership = await _membershipRepository.GetByUserAndGroupAsync(userId, invitation.GroupId, cancellationToken);
        if (membership is not null)
        {
            return new InvitationCommandResult(InvitationCommandStatus.AlreadyMember, null);
        }

        invitation.Status = "accepted";
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.InvitedUserId = userId;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        await _membershipRepository.AddAsync(new Membership
        {
            Id = Guid.NewGuid(),
            GroupId = invitation.GroupId,
            UserId = userId,
            Role = "member",
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _cacheService.RemoveAsync($"pending-invitations:{email}");
        await _cacheService.RemoveAsync($"groups:{userId}");

        return new InvitationCommandResult(InvitationCommandStatus.Success, new InvitationResponse
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            InvitedEmail = invitation.InvitedEmail,
            Status = invitation.Status
        });
    }

    public async Task<InvitationCommandResult> DeclineAsync(Guid userId, string email, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.Status != "pending")
        {
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (!string.Equals(invitation.InvitedEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        invitation.Status = "declined";
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.InvitedUserId = userId;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        await _cacheService.RemoveAsync($"pending-invitations:{email}");

        return new InvitationCommandResult(InvitationCommandStatus.Success, new InvitationResponse
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            InvitedEmail = invitation.InvitedEmail,
            Status = invitation.Status
        });
    }
}
