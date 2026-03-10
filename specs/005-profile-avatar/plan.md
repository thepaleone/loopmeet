# Implementation Plan: Profile Avatar Management

**Branch**: `005-profile-avatar` | **Date**: 2026-03-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-profile-avatar/spec.md`

## Summary

Add end-to-end profile avatar management to LoopMeet: automatically sync a social provider's profile photo on sign-in, display a circular avatar (or placeholder) on the Profile page, remove the manual URL entry fields, and allow users to replace their photo by taking a new one or choosing from their library, with the image uploaded via the backend API to Supabase cloud storage. Uses MAUI's built-in `MediaPicker` and `Permissions` APIs — no new packages required.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, Refit 10.0.1, Supabase 1.1.1
**Storage**: `UserProfileCache` (JSON in `Preferences`) — no schema changes; `UserProfileResponse.AvatarUrl` already exists
**Testing**: xUnit 2.9.3, source-inspection pattern (reads .cs/.xaml source files)
**Target Platform**: iOS 15+, Android API 21+, macOS 15+ (Catalyst), Windows 10
**Project Type**: Cross-platform mobile/desktop app (.NET MAUI)
**Performance Goals**: Avatar upload completes within 60 seconds on standard mobile connection (SC-003)
**Constraints**: Offline-tolerant — avatar sync failure must not block navigation; upload failure must preserve existing avatar
**Scale/Scope**: 4 platforms; 3 user stories; touches Auth, Profile, and Services layers

## Constitution Check

| Gate | Status | Notes |
| ------ | -------- | ------- |
| I. Code Quality | ✅ Pass | Source-inspection tests enforce structural quality; no dead code introduced |
| II. Tests Required | ✅ Pass | Source-inspection tests for each story; manifest changes verified by build |
| III. UX First | ✅ Pass | User scenarios and acceptance criteria defined in spec; permission denials handled gracefully |
| IV. Simplicity | ✅ Pass | No new packages; reuses MediaPicker and Permissions from MAUI Essentials; Grid overlay reused from HomePage |
| V. Modularity | ✅ Pass | New `PickAvatarCommand` isolated in `ProfileViewModel`; `UploadAvatarAsync` added to existing `IUsersApi` interface |
| VI. Contract-First | ✅ Pass | `contracts/api-avatar.md` defines `POST /users/avatar` and `PATCH /users/profile` changes before implementation |
| VII. Observability | ✅ Pass | Upload failures logged via `ILogger<ProfileViewModel>` (already injected); user-visible status message on failure |
| Privacy | ✅ Pass | Minimum permissions requested (per-use, not at launch); no avatar URL exposed in UI after this feature |

**Re-check post-design**: All gates still pass. The `SocialAvatarUrl` addition to `UserProfileUpdateRequest` is additive and backward-compatible. No circular dependencies introduced.

## Project Structure

### Documentation (this feature)

```text
specs/005-profile-avatar/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-avatar.md    # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (affected files)

```text
src/LoopMeet.App/
├── Features/
│   ├── Auth/
│   │   └── ViewModels/
│   │       └── LoginViewModel.cs          # MODIFIED: social avatar sync after OAuth sign-in
│   └── Profile/
│       ├── Models/
│       │   └── ProfileModels.cs           # MODIFIED: add SocialAvatarUrl to UserProfileUpdateRequest
│       ├── ViewModels/
│       │   └── ProfileViewModel.cs        # MODIFIED: remove AvatarInput/AvatarSource; add HasAvatar, IsUploading, PickAvatarCommand
│       └── Views/
│           └── ProfilePage.xaml           # MODIFIED: circular avatar, remove URL fields, add tap gesture
├── Services/
│   └── UsersApi.cs                        # MODIFIED: add UploadAvatarAsync (multipart)
└── Platforms/
    ├── iOS/Info.plist                     # MODIFIED: camera + photo library usage descriptions
    ├── MacCatalyst/Info.plist             # MODIFIED: camera + photo library usage descriptions
    └── Android/AndroidManifest.xml        # MODIFIED: CAMERA + READ_MEDIA_IMAGES permissions

tests/LoopMeet.App.Tests/
└── Features/
    └── Profile/
        └── ProfileViewModelTests.cs       # MODIFIED: new source-inspection tests for Stories 1-3
```

**Structure Decision**: Single-project MAUI app (existing layout). No new projects or modules added.

## Phase 0: Research Summary

All unknowns resolved. See [research.md](research.md) for full rationale.

| Question | Decision |
| ---------- | ---------- |
| Where does social avatar sync happen for returning users? | `LoginViewModel` post-OAuth, best-effort PATCH with `SocialAvatarUrl` |
| How to upload binary image via Refit? | `[Multipart]` + `StreamPart` — no new packages |
| Which photo picker API? | MAUI built-in `MediaPicker` (ships with MAUI Essentials) |
| How to check camera availability? | `MediaPicker.Default.IsCaptureSupported` |
| How to handle permissions per platform? | `Permissions.RequestAsync<T>()` per-use; manifests updated for iOS/Android/macOS |

## Phase 1: Design

### Story 1 — Social Login Avatar Sync

**Problem**: Returning Google users sign in and `OAuthSignInResult.AvatarUrl` is populated by `AuthService` but discarded after navigation to Home. Only new users pass it through to `CreateAccountViewModel`.

**Fix**: In `LoginViewModel`, after successful Google sign-in for a returning user, check if the cached profile has no avatar and the OAuth result has one. If so, call `UpdateProfileAsync` with `SocialAvatarUrl`. This is fire-and-forget (does not block navigation) and the backend only applies it if the stored avatar is currently null (FR-002).

**New `UserProfileUpdateRequest` field**: `string? SocialAvatarUrl` — backend applies only when stored avatar is null/empty.

### Story 2 — Profile Page Avatar Display

**Profile page layout change**: Replace the Avatar URL label + entry + source label block with the same `Border + Ellipse + Grid` circle used on the Home page. Place it to the left of the Display Name label + entry in a `HorizontalStackLayout`.

**ViewModel**: Remove `AvatarInput`, `AvatarSource`; add `HasAvatar` (bool). `SaveProfileAsync` no longer sends `AvatarOverrideUrl`.

### Story 3 — Tap-to-Replace Avatar

**`PickAvatarCommand`**: Async RelayCommand on `ProfileViewModel`. Shows `DisplayActionSheet` with "Take a new photo" (hidden if `!IsCaptureSupported`) and "Choose from library". Requests the minimum required platform permission before proceeding. On completion, opens `MediaPicker`, gets `FileResult`, streams it to `UploadAvatarAsync`, updates profile and cache on success, shows error status on failure.

**New API method**: `IUsersApi.UploadAvatarAsync([Multipart] StreamPart image)` returns `Task<UserProfileResponse>`.

**XAML**: `TapGestureRecognizer` on the avatar `Border` bound to `PickAvatarCommand`.

**Platform manifests**: iOS, macOS — `NSCameraUsageDescription` + `NSPhotoLibraryUsageDescription` in `Info.plist`. Android — `CAMERA` + `READ_MEDIA_IMAGES` + `READ_EXTERNAL_STORAGE` (max API 32) in manifest.

### Tests

Source-inspection pattern (consistent with existing test suite):

- `LoginViewModel_HasSocialAvatarSyncAfterGoogleSignIn` — asserts source contains `SocialAvatarUrl` and `AvatarUrl` check
- `ProfileViewModel_HasPickAvatarCommandWithMediaPicker` — asserts source contains `PickAvatarAsync`, `MediaPicker`, `IsCaptureSupported`
- `ProfileViewModel_DoesNotHaveAvatarInputOrAvatarSource` — asserts `AvatarInput` and `AvatarSource` removed
- `ProfilePage_HasCircularAvatarWithTapGesture` — asserts XAML contains `Ellipse`, `PickAvatarCommand`, `tab_profile_fallback.png`
- `ProfilePage_DoesNotHaveAvatarUrlEntry` — asserts XAML does not contain `AvatarInput` binding
- `UsersApi_HasUploadAvatarMethod` — asserts source contains `UploadAvatarAsync` and `StreamPart`
