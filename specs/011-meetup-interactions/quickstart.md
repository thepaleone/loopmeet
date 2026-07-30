# Quickstart: Validating Meetup Interaction Improvements

**Feature**: 011-meetup-interactions | **Date**: 2026-07-30

## Prerequisites

- Two test accounts in the same group: **Owner** (owns the group) and **Member** (non-owner). Both need at least one upcoming meetup visible on Home.
- A third scenario needs a **departed creator**: a meetup created by a member who is then removed from the group (research D1 / data-model INV-1).
- At least one meetup **with** a location (place name + coordinates) and one **without** any location.
- Android device/emulator plus iPhone if available; the save-row and map-launch rows are the platform-sensitive ones.

## Automated checks

```bash
dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true
```

Must cover, per research §3:

- **Api**: `CreatedByDisplayName` populated for a current member; empty for a departed creator; `GroupName` present on the group-scoped list (new); `GroupOwnerUserId` correct on both lists. Meetup endpoint tests must now seed `User` rows — they never did before, so a test that passes without seeding is asserting nothing.
- **Client unit**: `MeetupOrganizerText.Format` — passthrough for a real name; the FR-011 placeholder for null, empty, and whitespace.
- **Source inspection**: save icon on the title row and bottom button gone (both forms); card tap bound to the details command; location label's tap recognizer removed; map control bound to the maps command and gated on `CanOpenLocation`; details page pencil gated on `IsOwner`; `meetup-detail` route registered.

Build the Android app for the device rows:

```bash
dotnet build -c Staging -t:Run -f net10.0-android src/LoopMeet.App
```

## Validation matrix

| # | Story | Scenario | Steps | Expected |
| --- | --- | --- | --- | --- |
| 1 | US1 | Save reachable with keyboard up (create) | Group Detail → add meetup → tap into Title so the keyboard shows → tap the save icon top-right | Meetup is created; no scrolling and no keyboard dismissal needed at any point |
| 1a | US1 | No duplicate submit on double-tap | Fill in a valid new meetup → tap the save icon twice as fast as possible; repeat on Edit Meetup | Exactly one meetup created (check the group's list) / exactly one update applied. The icon is smaller than the button it replaces, so this is more reachable by accident than before (FR-004) |
| 2 | US1 | Save reachable with keyboard up (edit) | Open an existing meetup → edit → change Title with keyboard up → tap save icon | Change saved; same behavior as row 1 |
| 3 | US1 | Validation unchanged | Clear the Title → tap save icon | Same error message, in the same place as before; nothing saved |
| 4 | US1 | No duplicate save control | Scroll to the bottom of both forms | No bottom save button remains |
| 5 | US1 | Save row survives location search | Start typing in the location search (fields collapse) → look at the title row | Save icon still visible and functional; validation message if data is incomplete |
| 6 | US1 | Accessibility label | Enable the platform screen reader → focus the save icon | Purpose is announced ("Save meetup" / "Save changes"), not "button" alone |
| 7 | US2 | Home card opens details, promptly | Home → tap a meetup card (anywhere except the map control) → note how long until the fields are populated | Details screen opens for that meetup and its information appears within ~1 s on normal connectivity (SC-007) — the screen re-reads on every arrival, so this is a real network round trip, not a cached view |
| 8 | US2 | All five fields present | On the details screen | Title, date/time, location, group name, and organizer all visible without scrolling |
| 9 | US2 | Organizer resolves | View a meetup created by a current member | That member's display name is shown |
| 10 | US2 | Departed creator falls back | View the meetup whose creator was removed from the group | Shows "A group member" — never blank, never a raw id |
| 11 | US2 | Location TBD | View the meetup with no location | Location reads "TBD"; no map control on the card or the details screen |
| 12 | US2 | Map launch from details | Details screen for a located meetup → tap the map control | Native maps app opens at the correct place |
| 13 | US2 | Map launch from card | Home card for a located meetup → tap the card's map control | Native maps opens; the details screen does **not** open |
| 14 | US2 | Location text is no longer its own target | Tap the location text on a card | Details screen opens (not maps) |
| 15 | US2 | Back returns to origin | Open details from Home → back; open details from Group Detail → back | Returns to the originating screen each time |
| 16 | US3 | Owner reaches edit via details | As **Owner**, Group Detail → tap meetup → tap pencil | Details opens first (not the edit form); pencil opens the existing Edit Meetup form |
| 17 | US3 | Non-owner sees details, no pencil | As **Member**, Group Detail → tap the same meetup | Details opens (previously nothing happened); no pencil anywhere on the screen |
| 18 | US3 | Ownership is entry-point independent | As **Owner**, open the same meetup from **Home** instead | Pencil present, same as from Group Detail |
| 19 | US3 | Non-owner from Home | As **Member**, open a meetup from Home | Details opens; no pencil |
| 20 | US3 | Values fresh after edit | As **Owner**, edit the title from the details screen → save → return to details | Details shows the **new** title, not the pre-edit value |
| 21 | US3 | Swipe-to-delete unchanged (owner) | As **Owner**, swipe a meetup card on Group Detail | Delete behaves exactly as before |
| 22 | US3 | Swipe-to-delete unchanged (non-owner) | As **Member**, swipe a meetup card | No delete offered, exactly as before |
| 23 | Edge | Deleted while open | Open details as **Member**; have **Owner** delete that meetup; navigate back and re-open the same meetup (there is deliberately no pull-to-refresh — re-entry is the specified freshness mechanism, INV-5) | "No longer available" state; no pencil, no map control, no blank screen |
| 24 | Edge | Offline details load | Airplane mode → tap a meetup card | Retry-able error message; no partial data presented as current |
| 25 | Edge | Long values | View a meetup with a very long title and place name | Wraps or truncates; no field pushed off-screen |
| 26 | FR-010 | Name without coordinates | A meetup with a place name but no coordinates (seed directly if needed) | Name shown; no map control on card or details |
| 27 | FR-021 | Glyph rendering parity | Inspect the edit and map glyphs on Android and iOS | Both render as intended on both platforms (the reason glyphs were chosen over new assets) |

## Cache-behavior note (expected, not a defect)

`home-meetups:{userId}` is not invalidated on meetup writes (30-second TTL only) — pre-existing behavior, unchanged by this feature. So after an edit, the **Home list** may show the old title for up to 30 seconds while the **details screen** shows the new one immediately (it reads `meetups:{groupId}`, which *is* invalidated). Row 20 tests the details screen; do not treat a briefly stale Home card as a failure.

## Known reachability limit

Both list endpoints return upcoming meetups only, so the details screen can only display upcoming meetups. A meetup that passes while the app is open resolves to the "no longer available" state (row 23's presentation). Displaying past meetups would require a get-meetup-by-id endpoint — deliberately deferred (research D7/D8).
