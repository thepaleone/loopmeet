# Tasks: Profile Avatar Management

**Input**: Design documents from `/specs/005-profile-avatar/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to ([US1], [US2], [US3])

---

## Phase 1: Setup

**Purpose**: Establish baseline before any story work begins.

- [x] T001 Verify `dotnet test tests/LoopMeet.App.Tests/` passes (10 tests) on branch `005-profile-avatar` to confirm clean baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Model change required by Story 1 and backward-compatible with Stories 2 and 3.

**⚠️ CRITICAL**: T002 must complete before Phase 3 begins.

- [x] T002 Add `public string? SocialAvatarUrl { get; set; }` to `UserProfileUpdateRequest` in `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`

**Checkpoint**: Model updated — US1, US2, and US3 can now proceed independently.

---

## Phase 3: User Story 1 — Social Login Auto-Populates Avatar (Priority: P1) 🎯 MVP

**Goal**: When a returning Google user signs in and their profile has no avatar, the social provider's profile picture is automatically saved to their profile.

**Independent Test**: Sign in with a Google account that has a profile photo, with no existing avatar in the LoopMeet profile. Navigate to Home — the circular avatar should display the provider photo automatically.

### Tests for User Story 1

- [x] T003 [US1] Add source-inspection test `LoginViewModel_HasSocialAvatarSyncAfterGoogleSignIn` to `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs` — asserts `LoginViewModel.cs` source contains `SocialAvatarUrl`, `GetCachedProfile`, and `AvatarUrl` check

### Implementation for User Story 1

- [x] T004 [P] [US1] Read `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs` and locate the returning-user post-sign-in path in `SignInWithGoogleAsync` — add best-effort avatar sync: if `OAuthSignInResult.AvatarUrl` is non-empty and cached profile has no `AvatarUrl`, fire-and-forget `UpdateProfileAsync(new UserProfileUpdateRequest { DisplayName = cached?.DisplayName ?? string.Empty, SocialAvatarUrl = oauthResult.AvatarUrl })`
- [x] T005 [US1] Run `dotnet test tests/LoopMeet.App.Tests/` and confirm T003 test passes and no regressions

**Checkpoint**: Story 1 complete — social avatar sync working for returning users independently testable.

---

## Phase 4: User Story 2 — Avatar Displayed on Profile Page (Priority: P1)

**Goal**: Profile page shows the user's avatar (or a Gray400 placeholder circle) to the left of the display name. Raw avatar URL fields are removed.

**Independent Test**: Navigate to the Profile page as a user with an avatar — circular photo beside display name. As a user without an avatar — Gray400 circle with profile icon. Confirm the avatar URL text field and source label are absent.

### Tests for User Story 2

- [x] T006 [US2] Add three source-inspection tests to `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`:
  - `ProfileViewModel_DoesNotHaveAvatarInputOrAvatarSource` — asserts `ProfileViewModel.cs` does NOT contain `AvatarInput` or `AvatarSource`
  - `ProfilePage_HasCircularAvatarBesideDisplayName` — asserts `ProfilePage.xaml` contains `Ellipse`, `tab_profile_fallback.png`, `HasAvatar`
  - `ProfilePage_DoesNotHaveAvatarUrlEntry` — asserts `ProfilePage.xaml` does NOT contain `AvatarInput`

### Implementation for User Story 2

- [x] T007 [P] [US2] Update `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`:
  - Remove `_avatarInput` / `AvatarInput` property and its use in `Apply()`
  - Remove `_avatarSource` / `AvatarSource` property and its use in `Apply()`
  - Add `[ObservableProperty] private bool _hasAvatar;` updated in `Apply()` as `HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl)`
  - Remove `AvatarOverrideUrl = string.IsNullOrWhiteSpace(AvatarInput) ? null : AvatarInput` from `SaveProfileAsync` — save only sends `DisplayName`
- [x] T008 [US2] Update `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`:
  - Remove: Avatar URL `Label`, Avatar URL `Entry` (`Text="{Binding AvatarInput}"`), Avatar source `Label` (`Text="{Binding AvatarSource, ...}"`)
  - Add `HorizontalStackLayout` wrapping the display name section, with a `Border` (56×56, `StrokeShape=Ellipse`, `BackgroundColor={StaticResource Gray400}`) containing a `Grid` with `tab_profile_fallback.png` placeholder and `Image Source="{Binding AvatarUrl}" IsVisible="{Binding HasAvatar}"` on top — matching the pattern in `src/LoopMeet.App/Features/Home/Views/HomePage.xaml`
- [x] T009 [US2] Run `dotnet test tests/LoopMeet.App.Tests/` and confirm all T006 tests pass and no regressions

**Checkpoint**: Story 2 complete — Profile page displays circular avatar with placeholder independently testable.

---

## Phase 5: User Story 3 — User Replaces Their Avatar (Priority: P2)

**Goal**: Tapping the avatar circle on the Profile page presents a camera/library choice, requests appropriate platform permissions, and uploads the selected image via `POST /users/avatar`, updating the profile immediately.

**Independent Test**: On each platform — tap avatar, select source, grant permission, select/capture image, confirm it uploads and the new photo appears. Confirm denial and cancellation paths are handled gracefully.

### Tests for User Story 3

- [x] T010 [US3] Add two source-inspection tests to `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`:
  - `ProfileViewModel_HasPickAvatarCommandWithMediaPicker` — asserts `ProfileViewModel.cs` contains `PickAvatarAsync`, `MediaPicker`, `IsCaptureSupported`, `StreamPart`, `UploadAvatarAsync`
  - `UsersApi_HasUploadAvatarMethod` — asserts `UsersApi.cs` contains `UploadAvatarAsync` and `StreamPart`

### Implementation for User Story 3

- [x] T011 [P] [US3] Add multipart avatar upload to `src/LoopMeet.App/Services/UsersApi.cs`:
  - Add to `IUsersApi`: `[Multipart] [Post("/users/avatar")] Task<UserProfileResponse> UploadAvatarAsync([AliasAs("image")] StreamPart image);`
  - Add wrapper method `UploadAvatarAsync(StreamPart image)` to `UsersApi` class
- [x] T012 [P] [US3] Add camera and photo library usage descriptions to `src/LoopMeet.App/Platforms/iOS/Info.plist`:
  - `NSCameraUsageDescription`: "LoopMeet needs camera access to take a profile photo."
  - `NSPhotoLibraryUsageDescription`: "LoopMeet needs photo library access to choose a profile photo."
- [x] T013 [P] [US3] Add camera and photo library usage descriptions to `src/LoopMeet.App/Platforms/MacCatalyst/Info.plist` (same keys and values as T012)
- [x] T014 [P] [US3] Add camera and photo library permissions to `src/LoopMeet.App/Platforms/Android/AndroidManifest.xml`:
  - `<uses-permission android:name="android.permission.CAMERA" />`
  - `<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />`
  - `<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />`
- [x] T015 [US3] Add `PickAvatarAsync` RelayCommand and `IsUploading` bool property to `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` (depends on T011):
  - Check `MediaPicker.Default.IsCaptureSupported` to conditionally show "Take a new photo"
  - Use `DisplayActionSheet` for the choice prompt
  - Request `Permissions.Camera` before camera, `Permissions.Photos` before library
  - Show friendly alert on permission denial; return without throwing
  - On selection: open stream, call `_usersApi.UploadAvatarAsync(new StreamPart(...))`, call `Apply()` and update cache on success
  - On failure: log via `_logger`, set `StatusMessage` and `ShowStatus = true`
  - Set `IsUploading` true/false around the upload
- [x] T016 [US3] Add `TapGestureRecognizer` bound to `PickAvatarCommand` on the avatar `Border` in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml` (depends on T015)
- [x] T017 [US3] Run `dotnet test tests/LoopMeet.App.Tests/` and confirm all T010 tests pass and no regressions

**Checkpoint**: All 3 stories complete — avatar tap-to-replace works on all platforms independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T018 [P] Run full `dotnet test tests/LoopMeet.App.Tests/` and confirm all tests pass (expected: at least 16 tests)
- [ ] T019 [P] Manual acceptance testing on macOS (Mac Catalyst) per `specs/005-profile-avatar/quickstart.md` — all three story checklists
- [ ] T020 [P] Manual acceptance testing on iOS per `specs/005-profile-avatar/quickstart.md` — all three story checklists
- [ ] T021 [P] Manual acceptance testing on Android per `specs/005-profile-avatar/quickstart.md` — all three story checklists

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — blocks US1 only
- **US1 (Phase 3)**: Depends on T002
- **US2 (Phase 4)**: Independent of US1 — can start after Phase 1
- **US3 (Phase 5)**: Depends on US2 being complete (avatar must be visible to be tappable)
- **Polish (Phase 6)**: Depends on all story phases being complete

### User Story Dependencies

- **US1 (P1)**: Requires T002 (model field). Independent of US2 and US3.
- **US2 (P1)**: No cross-story dependencies. Can start in parallel with US1 after Phase 1.
- **US3 (P2)**: Requires US2 complete (Profile page avatar must be visible before adding tap gesture).

### Within Each Story

- Tests (T003, T006, T010) should be written first; confirm they fail before implementation
- T015 (PickAvatarAsync) depends on T011 (UploadAvatarAsync in UsersApi)
- T016 (tap gesture in XAML) depends on T015 (PickAvatarCommand on ViewModel)
- Manifest tasks T012, T013, T014 are parallel to each other and to T011

### Parallel Opportunities

```
Phase 3: T003 and T004 can run in parallel (different files)
Phase 4: T007 and T006 can run in parallel (different files)
Phase 5: T011, T012, T013, T014 can all run in parallel
Phase 5: T010 can run in parallel with T011–T014
Phase 6: T019, T020, T021 can run in parallel (different platforms)
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1: Baseline check
2. Complete Phase 2: Add `SocialAvatarUrl` to `UserProfileUpdateRequest`
3. Complete Phase 3: Social login avatar sync in `LoginViewModel`
4. **STOP and VALIDATE**: Sign in with Google on a fresh account — avatar appears on Home page
5. Ship if avatar display on Home is sufficient for MVP

### Incremental Delivery

1. Setup + Foundational → model ready
2. US1 → social login auto-populates avatar (zero-effort for social users)
3. US2 → Profile page shows avatar with placeholder (clean UI, no URL fields)
4. US3 → users can replace their photo (full self-service avatar management)
5. Each story adds value without breaking previous ones

---

## Notes

- All tests use source-inspection pattern (read `.cs`/`.xaml` files as strings) — consistent with existing suite in `tests/LoopMeet.App.Tests/`
- No new NuGet packages required — `MediaPicker` and `Permissions` ship with `Microsoft.Maui.Controls` 10.0.30
- `[P]` tasks touch different files; can be executed simultaneously
- US3 `PickAvatarAsync` is fire-and-forget for permission denial — never throws, always degrades gracefully
- Commit after each phase checkpoint or logical task group
