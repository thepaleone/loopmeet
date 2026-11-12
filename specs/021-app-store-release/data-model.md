# Data Model: App Store Release Build (TestFlight)

This feature stores no app data and changes no schema. The entities below are the release inputs and records it introduces.

## Build number

- **What:** an integer, `CFBundleVersion`, passed to the script as `-b`.
- **Rules:**
  - It's a positive integer.
  - App Store Connect accepts each version-and-build pair only once. A rejected, expired or abandoned build still uses up its number.
- **Relationships:**
  - It pairs with `ApplicationDisplayVersion` (`CFBundleShortVersionString`, `major.minor.patch`), which is shared with Android (015).
  - iOS and Android build numbers are counted separately.

## Signing inputs

| Field | Source | Rules |
| --- | --- | --- |
| Signing identity | `LOOPMEET_IOS_SIGNING_IDENTITY` | The SHA-1 fingerprint of a valid Apple Distribution certificate in the keychain. |
| Provisioning profile | `LOOPMEET_IOS_PROVISIONING_PROFILE` | An App Store profile for `io.loopmeet.app`, with `aps-environment` set to `production`, not expired, and including that certificate. |

The relationship between them: the profile's `DeveloperCertificates` must include the identity's certificate.

## TestFlight build

States in App Store Connect:

1. Uploaded.
2. Processing.
3. Ready for internal testing.
4. External testing only:
   1. Waiting for Beta App Review.
   2. Approved, then Ready for external testing.
5. Expired, 90 days after upload.

The quickstart records each upload's build number, display version and date.

## App Privacy answer (`web/app-privacy.md`)

| Field | Values |
| --- | --- |
| Data type | Apple's category and type, for example Contact Info → Email Address. |
| Policy section | The section of the privacy policy at `/privacy/` that describes it. |
| Linked to the user | Yes or No. |
| Used for tracking | Always No. |
| Purposes | Apple's purpose list. Only App Functionality applies today. |

Every data type the privacy policy names needs an answer row or an entry under "Not collected", and the same is true of `web/data-safety.md`.
