# Data Model: Home Tab Navigation Split

**Date**: 2026-02-26

## Scope Note

- This feature introduces UI/navigation restructuring only.
- No database schema changes or backend persistence changes are planned.
- Existing group and invitation data contracts are reused by new screen projections.

## Entities

### SignedInTabDefinition

- **Fields**: id, title, icon_key, icon_style, route, sort_order, is_default
- **Rules**:
  - `id` is unique within the signed-in tab bar.
  - `title` is required and user-visible.
  - `icon_key` is required for each tab (icon + text is mandatory).
  - `icon_style` supports emoji-style icon rendering or static image asset fallback.
  - Exactly one tab is marked `is_default=true` (Home for this feature).

### SignedInNavigationState

- **Fields**: active_tab_id, available_tabs, is_authenticated, launched_from_session_restore
- **Rules**:
  - `active_tab_id` must match an `available_tabs.id`.
  - When `is_authenticated=true` and the signed-in shell is first shown, `active_tab_id` defaults to Home.
  - Switching tabs must not invalidate authentication state.

### HomePlaceholderContent

- **Fields**: heading, supporting_text, status_label (optional)
- **Rules**:
  - `heading` and `supporting_text` are required.
  - Content must clearly communicate that the Home tab is reserved for a future feature.
  - Content is informational only for this release (no required backend interaction).

### GroupsScreenState

- **Fields**: owned_groups, member_groups, is_loading_owned, is_loading_member, is_busy, show_empty_state, error_message (optional)
- **Rules**:
  - `owned_groups` and `member_groups` default to empty collections.
  - `show_empty_state=true` only when both `owned_groups` and `member_groups` are empty.
  - Screen must not render pending invitation items in either collection.
  - Existing group item selection continues to route to group detail.

### InvitationsScreenState

- **Fields**: pending_invitations, is_loading, is_busy, show_empty_state, last_action_result (optional), error_message (optional)
- **Rules**:
  - `pending_invitations` contains only invitations with `status=pending`.
  - `show_empty_state=true` when `pending_invitations` is empty after load.
  - Accept/decline actions remove the completed invitation from `pending_invitations` after a successful refresh.
  - Invitation item selection can open the existing invitation detail flow.

### GroupSummary (reused)

- **Fields**: id, name, owner_user_id
- **Rules**:
  - Reused from existing app models and API responses.
  - Displayed only in the Groups screen for this feature.

### InvitationSummary (reused)

- **Fields**: id, group_id, group_name, owner_name, owner_email, sender_name, sender_email, invited_email, status, created_at (optional)
- **Rules**:
  - Reused from existing app models and API responses.
  - Invitations screen uses only records where `status=pending`.
  - Accept/decline transitions move items out of the pending projection.

## Relationships

- SignedInNavigationState 1..* SignedInTabDefinition
- GroupsScreenState 0..* GroupSummary (owned projection)
- GroupsScreenState 0..* GroupSummary (member projection)
- InvitationsScreenState 0..* InvitationSummary (pending projection)
- InvitationSummary -> GroupSummary (indirect relationship via `group_id` / `group_name`)

## State Transitions

- **Navigation tab selection**: Home <-> Groups <-> Invitations
- **Invitation status (existing backend behavior surfaced in UI)**: pending -> accepted | declined
- **Invitations screen projection**: pending item removed after successful accept/decline refresh

## Derived Views / Projections

- **Home Tab View**: Renders `HomePlaceholderContent` only; no groups or invitations list sections.
- **Groups Tab View**: Renders `GroupsScreenState` (owned + member groups) and groups empty state; excludes pending invitations.
- **Invitations Tab View**: Renders `InvitationsScreenState` and invitation empty state; excludes group lists.

## Validation Notes

- Tab bar presentation must include both icon and text for all three tabs.
- Icon placement should follow native tab bar layout (icon above text on supported mobile platforms).
- Animated GIF icons are not required and are intentionally avoided unless a platform-safe implementation is proven during implementation.
