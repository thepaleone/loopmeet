# Data Model: Profile Avatar Management (005-profile-avatar)

## Modified Models

### `UserProfileUpdateRequest` (PATCH /users/profile)
**File**: `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`

| Field | Type | Change | Notes |
|-------|------|--------|-------|
| `DisplayName` | `string` | Unchanged | |
| `AvatarOverrideUrl` | `string?` | Unchanged | User-explicitly-chosen URL (manual or uploaded) |
| `SocialAvatarUrl` | `string?` | **NEW** | Social provider photo URL; backend only applies if stored avatar is currently null |

`AvatarInput` (the editable string the user typed) is **removed** from `ProfileViewModel` and no longer sent to the API. The save action only sends `DisplayName`.

---

### `ProfileViewModel` changes
**File**: `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`

| Property / Command | Change | Notes |
|--------------------|--------|-------|
| `AvatarInput` | **Removed** | Manual URL entry removed |
| `AvatarSource` | **Removed** | Display-only label removed |
| `AvatarUrl` | **Kept** | Bound to the circular image |
| `HasAvatar` | **NEW** | `bool` — `!string.IsNullOrWhiteSpace(AvatarUrl)` |
| `IsUploading` | **NEW** | `bool` — shown during avatar upload |
| `PickAvatarCommand` | **NEW** | `[RelayCommand]` — triggers media picker action sheet |
| `SaveProfileCommand` | Modified | No longer sends `AvatarOverrideUrl` |

---

### `LoginViewModel` changes
**File**: `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs`

After `SignInWithGoogleAsync` returns for a **returning user** (navigating to Home, not create-account):
- If `OAuthSignInResult.AvatarUrl` is non-empty AND cached profile has no `AvatarUrl`, call `UsersApi.UpdateProfileAsync` with `SocialAvatarUrl` set.
- This is a fire-and-forget best-effort call; failure is logged but does not block navigation.

---

## New API Interface Method

### `IUsersApi` — new endpoint
**File**: `src/LoopMeet.App/Services/UsersApi.cs`

```
POST /users/avatar
Content-Type: multipart/form-data

Part name: "image"
Content: binary image data (JPEG or PNG)
Content-Type: image/jpeg | image/png

Response: UserProfileResponse (same as existing profile endpoints)
```

Refit declaration:
```csharp
[Multipart]
[Post("/users/avatar")]
Task<UserProfileResponse> UploadAvatarAsync([AliasAs("image")] StreamPart image);
```

The `UsersApi` wrapper class gets a corresponding `UploadAvatarAsync(StreamPart image)` method.

---

## Platform Manifest Changes

### iOS — `Platforms/iOS/Info.plist`
| Key | Value |
|-----|-------|
| `NSCameraUsageDescription` | "LoopMeet needs camera access to take a profile photo." |
| `NSPhotoLibraryUsageDescription` | "LoopMeet needs photo library access to choose a profile photo." |

### Android — `Platforms/Android/AndroidManifest.xml`
| Permission | Notes |
|------------|-------|
| `android.permission.CAMERA` | Camera capture |
| `android.permission.READ_MEDIA_IMAGES` | Photo library (API 33+) |
| `android.permission.READ_EXTERNAL_STORAGE` | Photo library (API < 33), `maxSdkVersion="32"` |

### macOS — `Platforms/MacCatalyst/Info.plist`
| Key | Value |
|-----|-------|
| `NSCameraUsageDescription` | "LoopMeet needs camera access to take a profile photo." |
| `NSPhotoLibraryUsageDescription` | "LoopMeet needs photo library access to choose a profile photo." |

### Windows
No manifest changes required. `MediaPicker` uses the system file picker which does not require explicit permissions.

---

## No New Entities

No new database tables or local storage schemas are introduced. The `UserProfileResponse.AvatarUrl` field already exists and continues to carry the avatar URL (whether social, uploaded, or override).

The `UserProfileCache` serialises/deserialises `UserProfileResponse` via JSON to `Preferences`; the `AvatarUrl` field is already cached and no schema migration is needed.
