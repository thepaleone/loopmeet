namespace LoopMeet.Core.Models;

/// <summary>The outcome of leaving a group, or of removing or blocking a member (feature 023).</summary>
public enum MembershipExitResult
{
    Done,
    NotGroupMember,
    OwnerCannotLeave,
    NotGroupOwner,
    CannotRemoveOwner
}
