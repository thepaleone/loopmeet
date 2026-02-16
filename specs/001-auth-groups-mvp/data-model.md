# Data Model: Auth & Groups MVP

**Date**: 2026-02-16

## Entities

### User

- **Fields**: id, display_name, email, phone (optional), created_at, updated_at
- **Rules**:
  - Email is required and unique.
  - Display name is required.
  - Phone is optional.

### Auth Identity

- **Fields**: id, user_id, provider, provider_subject, created_at
- **Rules**:
  - provider in {email, google} for MVP.
  - provider_subject unique per provider.

### Group

- **Fields**: id, owner_user_id, name, created_at, updated_at
- **Rules**:
  - Name is required.
  - Group name must be unique per owner_user_id.

### Membership

- **Fields**: id, group_id, user_id, role, created_at
- **Rules**:
  - role in {owner, member}.
  - owner_user_id must have a matching membership with role=owner.
  - user_id + group_id is unique.

### Invitation

- **Fields**: id, group_id, invited_email, invited_user_id (optional), status, created_at, accepted_at (optional)
- **Rules**:
  - status in {pending, accepted} for MVP.
  - invited_email is required.
  - If invited_user_id is known, it must match invited_email.
  - Only one pending invitation per invited_email per group.

## Relationships

- User 1..* Group (as owner)
- User *..* Group via Membership
- Group 1..* Invitation
- User 0..* Invitation (if invited_user_id is known)

## State Transitions

- Invitation: pending -> accepted

## Derived Views

- **Group list**: owned groups first, then member-only groups; sorted by group name within each section.
- **Pending invitations list**: all invitations for current user with status=pending.
