# Research: Profile Avatar Management (005-profile-avatar)

## Decision 1: Social Login Avatar Sync — Where and How

**Decision**: Sync the social avatar in `LoginViewModel` immediately after `SignInWithGoogleAsync` succeeds, for returning users only (new users already pass `SocialAvatarUrl` through the create-account flow via `AuthCoordinator.NavigateToCreateAccountAsync`).

**Rationale**: `AuthService.SignInWithGoogleAsync()` already extracts `AvatarUrl` from social provider metadata (`avatar_url` / `picture` fields). The `AuthCoordinator` already passes `socialAvatarUrl` to the create-account page for new users. The gap is returning users: after `ExchangeCodeForSession` the caller navigates directly to the Home tab with no avatar check.

Fix: in `LoginViewModel`, after a successful Google sign-in for a returning user, check `UserProfileCache.GetCachedProfile()?.AvatarUrl`. If null/empty and `OAuthSignInResult.AvatarUrl` is non-empty, call `UsersApi.UpdateProfileAsync` with a new `SocialAvatarUrl` field on `UserProfileUpdateRequest`. The backend applies it only when the stored avatar is currently null (FR-002).

**Alternative considered — server-side only**: Have the backend read the social avatar on every sign-in from the JWT. Rejected because the backend would need Supabase user-metadata access in the profile sync endpoint, which is outside the current API contract. Client-driven is simpler and consistent with how the create-account flow already works.

**Alternative considered — upsert vs patch**: Reuse `UpsertProfileAsync (POST /users/profile)` with `SocialAvatarUrl`. Rejected because a POST upsert for a returning user risks overwriting fields (DisplayName, Phone) that the user may have customised, if the client sends stale OAuth metadata.

---

## Decision 2: Avatar File Upload — API Endpoint Design

**Decision**: Add a new `POST /users/avatar` endpoint accepting `multipart/form-data` with a single `image` file part. Returns the full `UserProfileResponse` (matching the existing pattern of profile-mutating endpoints). The upload destination bucket/path is determined server-side from the API's Supabase configuration; the client receives the resulting public URL in `AvatarUrl`.

**Rationale**: The spec requires the cloud storage destination to be configurable via the API (FR-010). The client must not construct Supabase storage URLs directly. A dedicated endpoint keeps avatar upload concerns separate from profile field updates and allows the backend to handle resizing, content-type validation, and bucket configuration independently.

**Refit implementation**: `[Multipart]` attribute + `[AliasAs("image")] StreamPart image` parameter. MAUI `MediaPicker` returns a `FileResult`; open its stream and wrap in `StreamPart` with filename and content-type.

**Alternative considered — base64 JSON body**: Encode image as base64 in a JSON request. Rejected: larger payload, no streaming, breaks for large images, and is non-standard for binary uploads.

---

## Decision 3: Photo Picker and Camera — MAUI API

**Decision**: Use MAUI's built-in `MediaPicker` (`Microsoft.Maui.Media.MediaPicker.Default`) — no new NuGet packages required.

- **Camera**: `MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Take a photo" })`
- **Library**: `MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Choose a photo" })`
- **Camera availability check**: `MediaPicker.Default.IsCaptureSupported` — hide "Take a new photo" option if `false` (covers macOS and any platform/device without a camera)

`MediaPicker` is part of .NET MAUI Essentials (ships with `Microsoft.Maui.Controls`). No additional package reference required.

**Alternative considered — CommunityToolkit.Maui MediaPicker**: CT.Maui exposes a similar API. Rejected: the built-in MAUI Essentials `MediaPicker` is sufficient and avoids an unnecessary dependency bump.

---

## Decision 4: Platform Permissions

**Decision**: Use `Microsoft.Maui.ApplicationModel.Permissions` to request permissions at runtime before opening camera or photo library. Request only when needed (not at app start).

| Platform | Camera permission | Photo library permission |
|----------|------------------|--------------------------|
| Android | `Permissions.Camera` | `Permissions.Photos` (API 33+) or `Permissions.StorageRead` (<33) |
| iOS | `Permissions.Camera` | `Permissions.Photos` |
| macOS | `Permissions.Camera` | `Permissions.Photos` — macOS Catalyst maps through the same API |
| Windows | No runtime permission needed; `MediaPicker` uses the system file picker |

Required manifest additions:
- **iOS** (`Info.plist`): `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription`
- **Android** (`AndroidManifest.xml`): `CAMERA`, `READ_MEDIA_IMAGES` (API 33+) / `READ_EXTERNAL_STORAGE` (<33)
- **macOS** (entitlements / Info.plist): `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription`
- **Windows**: No additional manifest entries for file picker

**Pattern**: `var status = await Permissions.RequestAsync<Permissions.Camera>()` → check `PermissionStatus.Granted` before proceeding. On denial, show a user-friendly message and return without throwing.

---

## Decision 5: Profile Page Avatar Display

**Decision**: Reuse the exact same `Border + Ellipse + Grid` overlay pattern already implemented on the Home page — a 56×56 `Border` with `StrokeShape="Ellipse"`, `BackgroundColor="{StaticResource Gray400}"`, containing a `Grid` with the `tab_profile_fallback.png` placeholder underneath and the `AvatarUrl` `Image` on top, `IsVisible="{Binding HasAvatar}"`.

Position: to the left of the display name `Label` + `Entry`, inside a `HorizontalStackLayout`.

**Removes from ProfilePage.xaml**: Avatar URL `Label`, Avatar URL `Entry`, Avatar source `Label` (FR-005). The `AvatarInput` and `AvatarSource` properties are also removed from `ProfileViewModel` and the `SaveProfileCommand` no longer sends `AvatarOverrideUrl`.

**Tap-to-pick**: Wrap the `Border` in a `TapGestureRecognizer` bound to a new `PickAvatarCommand` on `ProfileViewModel`.

---

## Decision 6: UserProfileUpdateRequest — New SocialAvatarUrl Field

**Decision**: Add `string? SocialAvatarUrl` to `UserProfileUpdateRequest` (the PATCH request model). The backend applies it only when the current stored avatar is null/empty (implementing FR-002 server-side). The client sets this field only during the post-OAuth-login sync; it is never set during a normal profile save.

**Rationale**: `AvatarOverrideUrl` already exists on this model and semantically means "user explicitly chose this URL". `SocialAvatarUrl` is semantically distinct: "social provider suggested this URL, use it only as a fallback". Keeping them separate lets the backend enforce the right precedence rules.
