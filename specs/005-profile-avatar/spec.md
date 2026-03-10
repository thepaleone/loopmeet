# Feature Specification: Profile Avatar Management

**Feature Branch**: `005-profile-avatar`
**Created**: 2026-03-10
**Status**: Draft
**Input**: User description: "Feature number 5 will update the profile avatar functionality in the following ways. 1 - If a user logs on with a social account (e.g. Google) AND the user's avatar URL in the profile table is currently NULL or empty AND the social login has an avatar URL or equivalent it should update their profile record with the avatar url. 2 - Change the Profile page to show the avatar if it exists or the generic profile image the same way it is shown on the home page. It should also show the image to the left of the display name label and input box. 3 - The Avatar URL label, input box, and source label should be removed since the image will be displayed. 4 - Tapping the image should allow the user to add a new image. This should ask if they want to take a new photo or use an existing image. All device security considerations should be accounted for allowing the user to grant the permissions appropriate for the platform. If the user goes all the way through to take a new photo or pick a new image, the image should be uploaded via the API to the cloud storage. Cloud storage is provided by Supabase and should be configurable via the API."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Social Login Auto-Populates Avatar (Priority: P1)

When a user signs in with a social provider (e.g. Google) for the first time and their profile has no avatar yet, their social profile picture is automatically saved to their LoopMeet profile. On subsequent visits the Home and Profile pages immediately show their photo without any manual action required.

**Why this priority**: Zero-effort personalisation for the majority of social login users. It sets the foundation for all avatar display improvements and requires no user interaction to deliver value.

**Independent Test**: Create a new account via Google (or another social provider with a profile photo). Log in and navigate to the Profile page. The profile photo from the social provider should appear automatically.

**Acceptance Scenarios**:

1. **Given** a user with a social login account whose LoopMeet profile avatar is null or empty, **When** they sign in, **Then** their social provider's profile picture URL is saved to their LoopMeet profile record.
2. **Given** a user with a social login account whose LoopMeet profile already has an avatar URL, **When** they sign in, **Then** their existing avatar is preserved and the social photo is not overwritten.
3. **Given** a user whose social provider does not supply a profile photo, **When** they sign in, **Then** no avatar is saved and the generic placeholder is shown.

---

### User Story 2 - Avatar Displayed on Profile Page (Priority: P1)

The Profile page shows the user's avatar (or a generic placeholder if none exists) in a circular frame to the left of the display name. The raw avatar URL field and source label are removed from the page.

**Why this priority**: Directly improves the visual quality and usability of the Profile page. Together with Story 1, this delivers a complete end-to-end avatar experience for social login users with no extra steps.

**Independent Test**: Navigate to the Profile page as a user with an avatar — the circular image should appear beside the display name field. Navigate as a user without an avatar — the generic placeholder circle should appear instead.

**Acceptance Scenarios**:

1. **Given** a logged-in user with an avatar URL, **When** they open the Profile page, **Then** their photo is displayed in a circular frame to the left of the display name label and input field.
2. **Given** a logged-in user with no avatar URL, **When** they open the Profile page, **Then** a generic circular placeholder is displayed in the same position.
3. **Given** the Profile page is open, **Then** the avatar URL text field, its label, and the avatar source label are not present on the page.

---

### User Story 3 - User Replaces Their Avatar (Priority: P2)

A user can tap their avatar circle on the Profile page to replace it. The app asks whether they want to take a new photo or choose an existing one from their library. The app requests the minimum required permissions, and if the user completes the flow, uploads the new image and updates their profile.

**Why this priority**: Provides user agency over their identity photo. Depends on Story 2 (the tappable avatar must be visible first). Uploading requires API and cloud storage integration, making it the most technically complex story.

**Independent Test**: On each target platform (iOS, Android, macOS, Windows), tap the avatar circle and select both options. Verify permission prompts appear appropriately, the selected image uploads successfully, and the profile page immediately reflects the new avatar.

**Acceptance Scenarios**:

1. **Given** a user on the Profile page, **When** they tap their avatar circle, **Then** a prompt appears offering "Take a new photo" and "Choose from library".
2. **Given** the user selects "Take a new photo" and the app does not yet have camera permission, **When** the action is triggered, **Then** the platform's standard permission request dialog is shown before the camera opens.
3. **Given** the user selects "Choose from library" and the app does not yet have photo library access, **When** the action is triggered, **Then** the platform's standard permission request dialog is shown before the picker opens.
4. **Given** the user completes the photo or image selection, **When** they confirm, **Then** the image is uploaded to cloud storage via the API and the profile avatar URL is updated to the new image's location.
5. **Given** the user dismisses the camera or picker without selecting an image, **Then** their existing avatar is unchanged.
6. **Given** the upload fails (e.g. no network), **Then** the user sees a friendly error message and their existing avatar is unchanged.

---

### Edge Cases

- What happens when the user denies camera or photo library permission? The action is cancelled gracefully with a user-friendly message; no crash or unhandled state.
- What happens if the social provider avatar URL becomes invalid or returns a broken image? The generic placeholder is displayed instead.
- What happens if the image selected by the user is extremely large? The API handles oversized files and returns an appropriate error; the client surfaces a user-friendly message.
- What happens if the user is on a platform that does not support a camera (e.g. macOS desktop)? The "Take a new photo" option is hidden or disabled on that platform.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When a user authenticates via a social provider and their profile avatar is null or empty, the system MUST automatically retrieve the social provider's profile picture URL and save it to the user's profile record.
- **FR-002**: The system MUST NOT overwrite an existing avatar URL during social login if one is already stored.
- **FR-003**: The Profile page MUST display the user's avatar in a circular frame to the left of the display name label and input field.
- **FR-004**: The Profile page MUST display a generic circular placeholder when no avatar URL exists, consistent with the Home page avatar display.
- **FR-005**: The Profile page MUST NOT display an avatar URL text field, its label, or an avatar source label.
- **FR-006**: Tapping the avatar circle on the Profile page MUST present the user with a choice to take a new photo or choose from their photo library.
- **FR-007**: On platforms with a camera, the app MUST request camera permission before opening the camera; on platforms without a camera the "Take a new photo" option MUST be hidden or disabled.
- **FR-008**: The app MUST request photo library permission before opening the image picker, using the minimum permission scope available on the platform.
- **FR-009**: If the user completes the photo or image selection, the image MUST be uploaded to cloud storage via the API and the profile avatar URL MUST be updated to the uploaded image's URL.
- **FR-010**: The cloud storage destination used for avatar uploads MUST be configurable via the API rather than hardcoded on the client.
- **FR-011**: If the upload fails, the user MUST see a clear error message and the existing avatar MUST remain unchanged.
- **FR-012**: If the user dismisses the camera or picker without selecting an image, no change MUST be made to the profile.

### Key Entities

- **User Profile**: The stored record for a user, including their avatar URL. Extended to support avatar upload via cloud storage.
- **Avatar Image**: A photo or image chosen by the user, uploaded to cloud storage and referenced by URL in the profile.
- **Cloud Storage Configuration**: The upload endpoint and storage bucket details, provided by the API rather than hardcoded on the client.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Social login users with a provider profile photo see their avatar on the Home and Profile pages automatically after their first sign-in, with no manual steps required.
- **SC-002**: All four supported platforms (iOS, Android, macOS, Windows) display the circular avatar or placeholder on the Profile page; the raw URL field is absent on all platforms.
- **SC-003**: A user can complete the full avatar replacement flow (tap → choose source → capture/select → upload → see new image) in under 60 seconds on a standard mobile connection.
- **SC-004**: The app correctly requests and handles permission for camera and photo library on all platforms without crashes or unhandled errors for all grant/deny combinations.
- **SC-005**: Avatar upload failures surface a user-visible error message in 100% of failure cases; the previous avatar is never silently replaced with a broken or empty state.

## Assumptions

- Image resizing or compression before upload is handled server-side by the API; the client sends the raw selected image data. If the API imposes a file size limit, a client-side error message will inform the user.
- The API provides a dedicated endpoint to upload an avatar image and returns the resulting storage URL; the client does not construct storage URLs directly.
- "Configurable via the API" means the upload URL or storage bucket is returned by the API (e.g. via a config or profile endpoint), not set in a client-side config file.
- On Windows (desktop), camera capture may not be available via the platform's standard media picker; the "Take a photo" option will be hidden if the platform reports no camera.
- The social login avatar sync is performed by the app immediately after sign-in completes and before the user sees the Home page, using the identity information already returned by the authentication provider.
- Permission handling follows each platform's most restrictive guidelines (e.g. iOS limited photo library access, Android runtime permissions) to avoid rejection from the App Store and Play Store.
