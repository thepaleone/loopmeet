# Quickstart: Profile Avatar Management (005-profile-avatar)

## Prerequisites

- Feature branch `005-profile-avatar` checked out
- All previous features merged to main; `dotnet test` passes on current branch
- Backend API deployed with `POST /users/avatar` endpoint and `SocialAvatarUrl` support on `PATCH /users/profile`

---

## Phase 1 — Social Login Avatar Sync (Story 1)

### Step 1.1 — Extend `UserProfileUpdateRequest`

In `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`, add:
```csharp
public string? SocialAvatarUrl { get; set; }
```

### Step 1.2 — Sync avatar in LoginViewModel after Google sign-in

In `LoginViewModel.SignInWithGoogleAsync()`, after a successful OAuth sign-in that navigates the user to Home (not to create-account), add:
```csharp
if (!string.IsNullOrWhiteSpace(oauthResult.AvatarUrl))
{
    var cached = _userProfileCache.GetCachedProfile();
    if (string.IsNullOrWhiteSpace(cached?.AvatarUrl))
    {
        // Best-effort; don't block navigation on failure
        _ = _usersApi.UpdateProfileAsync(new UserProfileUpdateRequest
        {
            DisplayName = cached?.DisplayName ?? string.Empty,
            SocialAvatarUrl = oauthResult.AvatarUrl
        });
    }
}
```

### Step 1.3 — Verify

1. Sign in with a Google account that has a profile photo; profile in DB has no avatar yet.
2. Navigate to the Home page → avatar should appear in the circular frame.
3. Sign out and sign in again → avatar is NOT overwritten (FR-002).

---

## Phase 2 — Profile Page Avatar Display (Story 2)

### Step 2.1 — Update `ProfileViewModel`

- Remove `AvatarInput` property and its use in `SaveProfileAsync` (stop sending `AvatarOverrideUrl`)
- Remove `AvatarSource` property
- Add `HasAvatar` bool property: `!string.IsNullOrWhiteSpace(AvatarUrl)`
- Ensure `Apply(profile)` no longer sets `AvatarInput` or `AvatarSource`

### Step 2.2 — Update `ProfilePage.xaml`

- Remove: Avatar URL `Label`, Avatar URL `Entry`, Avatar source `Label`
- Add circular avatar (same `Border + Ellipse + Grid` pattern as `HomePage.xaml`) to the left of the display name section:

```xml
<HorizontalStackLayout Spacing="16" VerticalOptions="Start">
    <Border WidthRequest="56" HeightRequest="56"
            StrokeThickness="0"
            BackgroundColor="{StaticResource Gray400}">
        <Border.StrokeShape><Ellipse /></Border.StrokeShape>
        <Grid WidthRequest="56" HeightRequest="56">
            <Image Source="tab_profile_fallback.png" Aspect="AspectFit"
                   WidthRequest="32" HeightRequest="32"
                   HorizontalOptions="Center" VerticalOptions="Center" />
            <Image Source="{Binding AvatarUrl}" Aspect="AspectFill"
                   IsVisible="{Binding HasAvatar}" />
        </Grid>
    </Border>
    <VerticalStackLayout Spacing="4" VerticalOptions="Center">
        <Label Text="Display name" FontAttributes="Bold" />
        <Entry Text="{Binding DisplayName}" Placeholder="Display name" />
    </VerticalStackLayout>
</HorizontalStackLayout>
```

### Step 2.3 — Verify

1. Sign in as a user with an avatar URL → circular photo appears left of display name.
2. Sign in as a user without an avatar → Gray400 circle with profile icon appears.
3. Confirm the raw avatar URL fields are gone on all platforms.

---

## Phase 3 — Tap-to-Replace Avatar (Story 3)

### Step 3.1 — Platform manifest permissions

**iOS** (`Platforms/iOS/Info.plist`):
```xml
<key>NSCameraUsageDescription</key>
<string>LoopMeet needs camera access to take a profile photo.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>LoopMeet needs photo library access to choose a profile photo.</string>
```

**Android** (`Platforms/Android/AndroidManifest.xml`):
```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
```

**macOS** (`Platforms/MacCatalyst/Info.plist`): same keys as iOS.

### Step 3.2 — Add `UploadAvatarAsync` to `IUsersApi`

```csharp
[Multipart]
[Post("/users/avatar")]
Task<UserProfileResponse> UploadAvatarAsync([AliasAs("image")] StreamPart image);
```

Add a corresponding wrapper method to `UsersApi`.

### Step 3.3 — Add `PickAvatarCommand` to `ProfileViewModel`

```csharp
[RelayCommand]
private async Task PickAvatarAsync()
{
    bool canCapture = MediaPicker.Default.IsCaptureSupported;
    string action = await Shell.Current.DisplayActionSheet(
        "Change profile photo", "Cancel", null,
        canCapture ? "Take a new photo" : null,
        "Choose from library");

    FileResult? photo = null;
    if (action == "Take a new photo")
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await Shell.Current.DisplayAlert("Permission required",
                "Camera access is needed to take a photo.", "OK");
            return;
        }
        photo = await MediaPicker.Default.CapturePhotoAsync();
    }
    else if (action == "Choose from library")
    {
        var status = await Permissions.RequestAsync<Permissions.Photos>();
        if (status != PermissionStatus.Granted)
        {
            await Shell.Current.DisplayAlert("Permission required",
                "Photo library access is needed to choose a photo.", "OK");
            return;
        }
        photo = await MediaPicker.Default.PickPhotoAsync();
    }

    if (photo is null) return;

    IsUploading = true;
    try
    {
        await using var stream = await photo.OpenReadAsync();
        var part = new StreamPart(stream, photo.FileName, photo.ContentType);
        var updated = await _usersApi.UploadAvatarAsync(part);
        _userProfileCache.SetCachedProfile(updated);
        Apply(updated);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Avatar upload failed");
        ShowStatus = true;
        StatusMessage = "Unable to upload photo. Please try again.";
    }
    finally
    {
        IsUploading = false;
    }
}
```

### Step 3.4 — Wire up tap gesture in `ProfilePage.xaml`

Wrap the avatar `Border` in a `TapGestureRecognizer`:
```xml
<Border ...>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding PickAvatarCommand}" />
    </Border.GestureRecognizers>
    ...
</Border>
```

Optionally add a small camera overlay icon or visual indicator that the avatar is tappable.

### Step 3.5 — Verify on each platform

| Platform | Camera option | Library option | Permission denied | Upload failure |
|----------|--------------|----------------|-------------------|----------------|
| Android | ✓ visible | ✓ | Shows alert | Shows status message |
| iOS | ✓ visible | ✓ | Shows alert | Shows status message |
| macOS | hidden (`IsCaptureSupported=false`) | ✓ | Shows alert | Shows status message |
| Windows | hidden | ✓ (system file picker, no permission dialog) | n/a | Shows status message |

---

## Manual Acceptance Checklist

### Story 1 — Social login avatar sync
- [ ] New Google sign-in with provider photo → avatar auto-populated in profile
- [ ] Existing Google sign-in with provider photo and no stored avatar → avatar synced
- [ ] Existing Google sign-in where profile already has avatar → avatar NOT overwritten
- [ ] Google sign-in with no provider photo → no change, placeholder shown

### Story 2 — Profile page display
- [ ] Avatar visible in circle left of display name when URL exists
- [ ] Gray400 placeholder circle shown when no avatar
- [ ] Avatar URL field, label, and source label absent from Profile page
- [ ] Save still works (only sends DisplayName)

### Story 3 — Tap-to-replace
- [ ] Tapping avatar circle shows action sheet with options
- [ ] "Take a new photo" hidden on macOS/no-camera devices
- [ ] Camera permission requested on first camera use; works on grant, aborts gracefully on deny
- [ ] Photo library permission requested on first use; works on grant, aborts gracefully on deny
- [ ] After successful upload, new photo appears immediately in the circle
- [ ] Upload failure shows error message; previous avatar preserved
- [ ] Dismissing without selection leaves avatar unchanged
