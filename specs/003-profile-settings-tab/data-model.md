# Data Model: Profile Tab and Profile Management

**Date**: 2026-02-26

## Scope Note

- This feature adds one new signed-in UI tab and expands the user profile domain.
- Data model changes are additive to existing profile and membership data.
- Existing groups/invitations data models remain intact.

## Entities

### ProfileTabDefinition

- **Purpose**: Defines the signed-in `Profile` tab metadata and route placement.
- **Fields**:
  - `id` (string, fixed: `profile`)
  - `title` (string, fixed: `Profile`)
  - `route` (string, fixed: `//profile`)
  - `sortOrder` (integer; must be greater than existing signed-in tab sort values)
  - `iconKey` (string)
- **Validation Rules**:
  - `id`, `title`, and `route` are required.
  - `sortOrder` must place `Profile` at the end of signed-in tab order.

### UserProfileAggregate

- **Purpose**: Represents editable profile and display projection fields required by the profile screen.
- **Fields**:
  - `id` (UUID)
  - `displayName` (string)
  - `email` (string)
  - `phone` (string, nullable)
  - `createdAt` (datetime; shown as `User since`)
  - `updatedAt` (datetime)
  - `socialAvatarUrl` (string, nullable)
  - `avatarOverrideUrl` (string, nullable)
  - `avatarSource` (enum: `none`, `social`, `user_override`)
  - `effectiveAvatarUrl` (derived string, nullable)
  - `groupCount` (derived integer)
- **Validation Rules**:
  - `displayName` is required and trimmed.
  - `socialAvatarUrl` and `avatarOverrideUrl`, when provided, must be valid absolute URLs.
  - `avatarSource=user_override` requires `avatarOverrideUrl`.
  - `groupCount` must be `>= 0`.

### AvatarSourceState

- **Purpose**: Captures avatar precedence logic and transition rules.
- **Fields**:
  - `socialAvatarUrl` (string, nullable)
  - `avatarOverrideUrl` (string, nullable)
  - `effectiveAvatarUrl` (derived string, nullable)
  - `source` (enum: `none`, `social`, `user_override`)
- **Derivation Rules**:
  - If `avatarOverrideUrl` exists -> `source=user_override`, `effectiveAvatarUrl=avatarOverrideUrl`.
  - Else if `socialAvatarUrl` exists -> `source=social`, `effectiveAvatarUrl=socialAvatarUrl`.
  - Else -> `source=none`, `effectiveAvatarUrl=null`.

### PasswordChangeRequest

- **Purpose**: Represents a modal-submitted password update action.
- **Fields**:
  - `currentPassword` (string)
  - `newPassword` (string)
  - `confirmPassword` (string)
- **Validation Rules**:
  - All fields required.
  - `newPassword == confirmPassword`.
  - `newPassword` must satisfy configured password policy.
  - Invalid current password returns validation failure without mutating profile fields.

### ProfileSummaryProjection

- **Purpose**: Read model for profile page display.
- **Fields**:
  - `displayName`
  - `effectiveAvatarUrl`
  - `avatarSource`
  - `userSince`
  - `groupCount`
  - `canChangePassword` (boolean)
- **Validation Rules**:
  - `userSince` always maps from `createdAt`.
  - `groupCount` is derived from current memberships and must remain non-negative.

## Relationships

- `UserProfileAggregate (1) -> (0..1) AvatarSourceState`
- `UserProfileAggregate (1) -> (0..*) Membership` (existing model; used for `groupCount` projection)
- `PasswordChangeRequest` applies to exactly one authenticated `UserProfileAggregate`.
- `ProfileTabDefinition` links to `ProfileSummaryProjection` as its screen data dependency.

## State Transitions

### Avatar Source Transition

- `none -> social`: social-login bootstrap provides avatar and no override exists.
- `social -> user_override`: user uploads/selects avatar from profile page.
- `none -> user_override`: user sets avatar without social avatar.
- `user_override -> user_override`: social-login bootstrap runs later; override remains unchanged.

### Password Modal Transition

- `closed -> open`: user taps Change Password on profile page.
- `open -> submitting`: user submits modal with required fields.
- `submitting -> success -> closed`: password update succeeds; success feedback shown.
- `submitting -> validation_error -> open`: policy/current-password mismatch; modal remains open with message.
- `open -> closed`: user cancels modal; no password mutation.

## Migration Notes

- Additive profile persistence changes are required to support avatar-source precedence:
  - add `social_avatar_url` (nullable)
  - add `avatar_override_url` (nullable)
  - optionally add persisted `avatar_source` if explicit source storage is preferred over derivation
- Existing rows default to `null` avatar fields and derive `avatarSource=none`.

## Testability Hooks

- Profile projection is deterministic from persisted profile + membership count and can be unit/integration tested.
- Avatar precedence behavior is deterministic and should be covered by API-level regression tests.
- Password change validation paths are deterministic and should be covered by endpoint tests for success and failures.
