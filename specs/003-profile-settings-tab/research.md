# Research: Profile Tab and Profile Management

**Date**: 2026-02-26

## Decision 1: Append `Profile` as a fourth signed-in Shell tab

- **Decision**: Add `Profile` as the last tab in `AppShell` and extend signed-in route constants to include `profile`/`//profile`.
- **Rationale**: Existing signed-in navigation already uses MAUI Shell tabs (`Home`, `Groups`, `Invitations`), so appending a fourth tab is the smallest, lowest-risk change and satisfies the explicit tab-order requirement.
- **Alternatives considered**: Replace an existing tab; place profile under flyout/settings route instead of tab bar; present profile from Home CTA only.

## Decision 2: Expand profile API response instead of composing profile data from multiple client calls

- **Decision**: Extend profile read contract to return `userSince`, `groupCount`, `avatarUrl`, and `avatarSource` in one payload.
- **Rationale**: The profile screen requires these values together. A single profile projection avoids duplicate app orchestration and inconsistent states between profile identity data and membership count.
- **Alternatives considered**: Derive group count client-side from `/groups`; create separate `/users/profile/summary` endpoint; keep current minimal profile response and fetch supplemental values independently.

## Decision 3: Use explicit avatar source precedence with persisted override metadata

- **Decision**: Persist both social avatar and user override avatar context, and compute effective avatar with precedence `user_override > social > none`.
- **Rationale**: This directly satisfies two requirements: user-selected avatar must override social identity avatar, and social avatar should populate default profile only when override is not established.
- **Alternatives considered**: Store a single avatar field only (cannot preserve source intent); recompute source from latest login metadata each sign-in (would overwrite user intent); keep avatar entirely client-side (not reliable across devices).

## Decision 4: Model password change as a dedicated operation separate from profile upsert

- **Decision**: Define a dedicated password change contract (`POST /users/password`) used from a popup modal workflow.
- **Rationale**: Password updates are security-sensitive and semantically distinct from profile attribute edits. A dedicated operation aligns with required modal UX and allows specific validation/error handling paths.
- **Alternatives considered**: Continue sending password through profile upsert; inline password fields on profile page; handle password update only in app without a dedicated backend contract.

## Decision 5: Copy social avatar during profile bootstrap only when override is absent

- **Decision**: During social-login profile create/bootstrap, include social avatar metadata in the profile upsert/update request; backend applies it only if no user override exists.
- **Rationale**: Existing OAuth bootstrap already upserts profile fields. Extending this pathway with conditional social avatar copy preserves current flow and enforces the required non-overwrite rule.
- **Alternatives considered**: Separate bootstrap endpoint; always overwrite from social provider on each OAuth sign-in; never copy social avatar (manual upload only).

## Decision 6: Require test updates for every changed behavior

- **Decision**: Add and update automated tests in app/API suites for new tab behavior, profile projection fields, avatar precedence logic, password change handling, and social-avatar bootstrap conditions.
- **Rationale**: Constitution Principle II requires automated tests for new behavior; user also explicitly requested implementing/changing tests for any changed functionality.
- **Alternatives considered**: Manual validation only; partial test coverage for only UI changes; defer tests to a follow-up task.

## Resolved Clarifications

- No unresolved `NEEDS CLARIFICATION` items remain after research.
