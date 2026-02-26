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
    private readonly ILogger<InvitationCommandService> _logger;

    public InvitationCommandService(
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository invitationRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        ILogger<InvitationCommandService> logger)
    {
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<InvitationCommandResult> CreateAsync(Guid ownerUserId, Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        var trimmedEmail = email.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            _logger.LogWarning("Create invitation invalid email for {GroupId} by {UserId}", groupId, ownerUserId);
            return new InvitationCommandResult(InvitationCommandStatus.InvalidEmail, null);
        }

        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            _logger.LogWarning("Create invitation group not found {GroupId} by {UserId}", groupId, ownerUserId);
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (group.OwnerUserId != ownerUserId)
        {
            _logger.LogWarning("Create invitation forbidden {GroupId} by {UserId}", groupId, ownerUserId);
            return new InvitationCommandResult(InvitationCommandStatus.Forbidden, null);
        }

        var existingUser = await _userRepository.GetByEmailAsync(trimmedEmail, cancellationToken);
        if (existingUser is not null)
        {
            var membership = await _membershipRepository.GetByUserAndGroupAsync(existingUser.Id, groupId, cancellationToken);
            if (membership is not null)
            {
                _logger.LogWarning("Create invitation already member {GroupId} by {UserId}", groupId, ownerUserId);
                return new InvitationCommandResult(InvitationCommandStatus.AlreadyMember, null);
            }
        }

        if (await _invitationRepository.ExistsPendingForEmailAsync(groupId, trimmedEmail, cancellationToken))
        {
            _logger.LogWarning("Create invitation duplicate {GroupId} by {UserId}", groupId, ownerUserId);
            return new InvitationCommandResult(InvitationCommandStatus.Duplicate, null);
        }

        _logger.LogInformation("Creating invitation for {GroupId} by {UserId}", groupId, ownerUserId);
        var now = DateTimeOffset.UtcNow;
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            InvitedByUserId = ownerUserId,
            InvitedEmail = trimmedEmail,
            InvitedUserId = existingUser?.Id,
            Status = "pending",
            CreatedAt = now
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        await _cacheService.RemoveAsync($"pending-invitations:{trimmedEmail}");
        _logger.LogInformation("Created invitation {InvitationId} for {GroupId} by {UserId}",
            invitation.Id,
            groupId,
            ownerUserId);

        var owner = await _userRepository.GetByIdAsync(ownerUserId, cancellationToken);

        return new InvitationCommandResult(InvitationCommandStatus.Success, new InvitationResponse
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            GroupName = group.Name,
            OwnerName = owner?.DisplayName ?? string.Empty,
            OwnerEmail = owner?.Email ?? string.Empty,
            // Current business rule: only the owner can invite members.
            SenderName = owner?.DisplayName ?? string.Empty,
            SenderEmail = owner?.Email ?? string.Empty,
            InvitedEmail = invitation.InvitedEmail,
            Status = invitation.Status,
            CreatedAt = invitation.CreatedAt
        });
    }

    public async Task<InvitationCommandResult> AcceptAsync(Guid userId, string email, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.Status != "pending")
        {
            _logger.LogWarning("Accept invitation not found {InvitationId} by {UserId}", invitationId, userId);
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (!string.Equals(invitation.InvitedEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Accept invitation email mismatch {InvitationId} by {UserId}", invitationId, userId);
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        var membership = await _membershipRepository.GetByUserAndGroupAsync(userId, invitation.GroupId, cancellationToken);
        if (membership is not null)
        {
            _logger.LogWarning("Accept invitation already member {InvitationId} by {UserId}", invitationId, userId);
            return new InvitationCommandResult(InvitationCommandStatus.AlreadyMember, null);
        }

        _logger.LogInformation("Accepting invitation {InvitationId} by {UserId}", invitationId, userId);
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
        _logger.LogInformation("Accepted invitation {InvitationId} by {UserId}", invitationId, userId);

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
            _logger.LogWarning("Decline invitation not found {InvitationId} by {UserId}", invitationId, userId);
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        if (!string.Equals(invitation.InvitedEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Decline invitation email mismatch {InvitationId} by {UserId}", invitationId, userId);
            return new InvitationCommandResult(InvitationCommandStatus.NotFound, null);
        }

        _logger.LogInformation("Declining invitation {InvitationId} by {UserId}", invitationId, userId);
        invitation.Status = "declined";
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.InvitedUserId = userId;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        await _cacheService.RemoveAsync($"pending-invitations:{email}");
        _logger.LogInformation("Declined invitation {InvitationId} by {UserId}", invitationId, userId);

        return new InvitationCommandResult(InvitationCommandStatus.Success, new InvitationResponse
        {
            Id = invitation.Id,
            GroupId = invitation.GroupId,
            InvitedEmail = invitation.InvitedEmail,
            Status = invitation.Status
        });
    }
}
