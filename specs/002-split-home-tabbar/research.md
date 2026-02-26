# Research: Home Tab Navigation Split

**Date**: 2026-02-26

## Decision 1: Use MAUI Shell `TabBar` for signed-in navigation

- **Decision**: Implement the signed-in navigation using the existing MAUI Shell with a `TabBar` that exposes separate Home, Groups, and Invitations tabs.
- **Rationale**: The app already uses `Shell` routing and `GoToAsync`, so a `TabBar` keeps navigation consistent, minimizes custom UI work, and preserves platform-native tab behavior (including icon-above-title layout on supported mobile platforms).
- **Alternatives considered**: Custom bottom navigation control in XAML; nested navigation pages with manual tab state management.

## Decision 2: Prefer emoji-style tab icons, not animated GIF tab icons

- **Decision**: Provide tab icons plus text labels for all tabs, using emoji-style icons rendered as reliable tab icons (for example `FontImageSource` glyphs or bundled image assets). Do not rely on animated GIF icons in the native tab bar.
- **Rationale**: The user requested icon + text with icon above text and preferred GIF or emoji. Native tab bars in MAUI Shell are more reliable with static image/icon sources, while animated GIF behavior in tab bars is inconsistent and can fail to animate or render across platforms. Emoji-style static icons meet the visual requirement with lower risk.
- **Alternatives considered**: Animated GIF icons in tab bar (higher platform inconsistency risk); text-only tabs (does not meet user preference); custom animated tab bar (unnecessary complexity).

## Decision 3: Split the current combined groups page into dedicated Groups and Invitations screens

- **Decision**: Create a dedicated invitations list screen (and view model) for pending invitations and refactor the current groups list screen to show groups-only content while preserving existing group actions.
- **Rationale**: This directly satisfies the feature goal, reduces screen clutter, and lowers regression risk by reusing existing group and invitation models/services instead of rewriting data flows.
- **Alternatives considered**: Keep one page and hide/show sections by segmented control (does not create tab-based navigation); duplicate logic in two pages without shared model/service reuse (increases maintenance cost).

## Decision 4: Reuse existing API endpoints without backend changes

- **Decision**: Keep backend contracts unchanged and use existing endpoints: `GET /groups`, `GET /invitations`, `GET /groups/{groupId}`, `POST /invitations/{invitationId}/accept`, and `POST /invitations/{invitationId}/decline`.
- **Rationale**: The feature is UI-only. Existing endpoints already support the required data and actions, so avoiding backend changes preserves simplicity and accelerates delivery.
- **Alternatives considered**: Add a new groups-only endpoint; split `GET /groups` response schema to remove invitations immediately; introduce a new aggregated home endpoint (not needed for placeholder home screen).

## Decision 5: Add app-focused automated tests for the split behavior

- **Decision**: Plan for app-level automated tests (ViewModel/logic tests) covering groups-only and invitations-only state handling, plus manual MAUI smoke tests for tab rendering and navigation.
- **Rationale**: The constitution requires automated tests for new behavior, and this feature’s regression risk is concentrated in view-model state separation and navigation routing rather than backend logic.
- **Alternatives considered**: Manual QA only (insufficient per constitution); UI automation end-to-end only (higher setup cost for this scope).
