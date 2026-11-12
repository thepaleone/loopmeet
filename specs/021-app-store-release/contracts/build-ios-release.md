# Contract: `deploy/build-ios-release.sh`

## Usage

```
deploy/build-ios-release.sh -b <build-number>
```

| Input | Kind | Rule |
| --- | --- | --- |
| `-b` | argument | A positive integer with no leading zero. It becomes `CFBundleVersion`, and App Store Connect rejects one already used for this version. |
| `LOOPMEET_IOS_SIGNING_IDENTITY` | environment | The 40-character hexadecimal SHA-1 of a valid Apple Distribution identity in the login keychain. |
| `LOOPMEET_IOS_PROVISIONING_PROFILE` | environment | The path to an App Store provisioning profile for `io.loopmeet.app` (see the checks below). |

Neither variable is a secret. The private key never leaves the keychain.

## Pre-build checks

Each check exits with status 1 and prints a message naming the problem. When any check fails, no build runs and nothing is written to `artifacts/`.

| # | Condition | Message names |
| --- | --- | --- |
| 1 | `-b` is missing or not a positive integer | the build-number rule, plus usage |
| 2 | Either variable is unset | the variable |
| 3 | The identity isn't 40 hexadecimal characters | the expected format, and where to find it (`security find-identity -v -p codesigning`) |
| 4 | The identity isn't a valid codesigning identity in the keychain | the identity, and that it's missing, expired or revoked |
| 5 | The profile file doesn't exist or can't be decoded | the path |
| 6 | The profile's App ID isn't `<team>.io.loopmeet.app` | the App ID it found |
| 7 | The profile isn't for App Store distribution: it lists devices, or its `aps-environment` isn't `production` | which of the two |
| 8 | The profile has expired | its expiry date |
| 9 | The profile doesn't include the identity's certificate | the identity |

## Output

- **On success:** `artifacts/ios/<n>/LoopMeet.App.ipa`. The exact file name comes from the SDK, and the script prints the path.
- **Post-build checks:** the script reads the built app. If `CFBundleVersion` isn't `<n>`, or the signed `aps-environment` isn't `production`, it exits with status 1.
- **`artifacts/`** is git-ignored.

## Side effects

- Copies the profile as `<UUID>.mobileprovision` into `~/Library/MobileDevice/Provisioning Profiles/` and `~/Library/Developer/Xcode/UserData/Provisioning Profiles/`, creating them if needed. This is safe to repeat, because the file name is the profile's UUID.
- Runs `dotnet clean` for `net10.0-ios` Release before publishing.
