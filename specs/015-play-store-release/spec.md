# Feature Specification: Google Play Release Build

**Feature Branch**: `015-play-store-release`  
**Created**: 2026-11-02  
**Status**: Draft  
**Input**: User description: "Begin the publish process to the Google Play store. This first version is for invited users only but is the foundation of a full workflow to the store. Apple comes later, but decisions that benefit both should account for it."

## Background

The app has only ever been installed with `deploy/deploy-android.sh` (an APK over adb). Google Play needs an Android App Bundle (`.aab`) signed with an upload key, a build number that rises with every upload, and a manifest that passes Play's permission policies. The app is registered in the Play Console as `io.loopmeet.app`; that package name is permanent and is also the intended iOS bundle ID.

The first audience is invited testers (Play internal testing, then closed testing). The Api and Supabase project the app talks to are still the staging ones; a production environment is out of scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A maintainer produces an uploadable bundle with one command (Priority: P1)

A maintainer with the upload key sets four environment variables and runs `deploy/build-android-release.sh -b <build-number>`; they get a signed `.aab` that Play accepts, without typing a password on the command line or storing one in the repo.

**Independent Test**: Run the script with a throwaway keystore; confirm the bundle is signed by that key and carries the given build number and the project's display version.

**Acceptance Scenarios**:

1. **Given** the signing variables are set, **When** the script runs with `-b 1`, **Then** it writes `artifacts/android/1/io.loopmeet.app-Signed.aab` signed by the upload key.
2. **Given** a missing variable, keystore file, `google-services.json`, or a non-numeric build number, **When** the script runs, **Then** it stops before building and names what is missing.
3. **Given** the bundle is uploaded to internal testing, **When** a tester installs it from Play, **Then** the app launches and works as the `deploy-android.sh` Release build does.

### User Story 2 - The bundle passes Play's permission policies (Priority: P1)

The app requests only permissions it uses, so review does not reject it under the photo and video permissions policy.

**Independent Test**: Inspect the merged manifest in the bundle; pick a profile photo from the library and use the meetup location search on a device.

**Acceptance Scenarios**:

1. **Given** the bundle, **When** its merged manifest is inspected, **Then** it does not request `READ_MEDIA_IMAGES`, `READ_MEDIA_VIDEO`, `MANAGE_EXTERNAL_STORAGE` or `ACCESS_BACKGROUND_LOCATION`.
2. **Given** Android 13 or later, **When** the user picks a profile photo from the library, **Then** the system photo picker opens with no permission prompt and the avatar uploads.
3. **Given** location permission is granted, **When** the user searches for a meetup location, **Then** results are biased to nearby places as before.

### User Story 3 - Versions mean the same thing on Android and iOS (Priority: P2)

A maintainer reads one version scheme in the project file that applies to both stores.

**Acceptance Scenarios**:

1. **Given** the project file, **Then** `ApplicationDisplayVersion` is `major.minor.patch` and `ApplicationVersion` is a positive integer build number, documented as shared by Android and iOS.

### Edge Cases

- `ACCESS_FINE_LOCATION` stays: MAUI's `Permissions.LocationWhenInUse` requires both coarse and fine to be declared and throws `PermissionException` otherwise, which would silently disable the location bias. Lookups request `Medium` accuracy, and Play does not restrict foreground location.
- `READ_EXTERNAL_STORAGE` (capped at API 32) stays: the profile photo flow requests `StorageRead` before picking, which needs it on Android 12 and earlier. Play's photo policy applies to the Android 13 media permissions, not this one.
- Build numbers are never reused. A rejected or abandoned upload still consumes its number.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repo MUST provide a script that builds a signed Release `.aab` for `io.loopmeet.app`, taking the build number as an argument and the upload key path, alias and passwords from environment variables (passwords via MSBuild's `env:` prefix).
- **FR-002**: The release build MUST keep the AOT and trimming workaround used by `deploy-android.sh` until that crash is resolved separately.
- **FR-003**: The manifest MUST NOT request `READ_MEDIA_IMAGES`, and MUST keep both foreground location permissions.
- **FR-004**: The project MUST use a semantic `ApplicationDisplayVersion` (starting at `0.1.0`) and an integer `ApplicationVersion` build number, shared by Android and iOS.
- **FR-005**: Keystores and release output MUST be git-ignored.
- **FR-006**: The Android minimum OS version MUST be API 24 (Android 7.0) or higher; Play automatic protection rejects bundles with a lower `minSdkVersion`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The first bundle is accepted by Play internal testing and installs on a tester's device from Play.
- **SC-002**: 100% of existing automated tests still pass, plus tests guarding the manifest, version scheme, script and ignore rules.

## Assumptions

- The upload key is created and kept by the maintainer outside the repo; Play App Signing holds the app signing key.
- Account deletion, privacy policy coverage, the support/deletion website, a production backend, re-enabling AOT, and CI-built releases are separate work.
