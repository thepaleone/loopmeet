# Quickstart: Scene Lifecycle — Device Validation

**Feature**: 013-uiscene-lifecycle | **Date**: 2026-10-30

The automated tests pin the configuration (scene manifest, `SceneDelegate` registration, MAUI version, minimum OS versions). This checklist covers what only a real launch can show. Run it once a .NET iOS/Mac Catalyst workload that supports your installed Xcode (27.2) is available.

## Prerequisites

1. Install the workload matching Xcode 27.2 (`dotnet workload update`), then confirm with `dotnet workload list`.
2. Clean old build output so the simulator doesn't run a stale build: delete `src/LoopMeet.App/bin` and `src/LoopMeet.App/obj`, and uninstall LoopMeet from the simulator and devices.
3. Build:
   ```sh
   dotnet build -f net10.0-ios src/LoopMeet.App
   dotnet build -f net10.0-maccatalyst src/LoopMeet.App
   ```
4. Have a staging account that can sign in with Google and one with Apple, and a group with at least one upcoming meetup.

## Matrix

Run on an **iOS 27 simulator or iPhone**, an **iPad or iPad simulator**, and **Mac Catalyst**. Mark ✅/❌. Run each row in Debug and repeat rows 1, 2 and 7 with a Release build.

| # | Steps | Expected | iPhone | iPad | Mac |
| --- | --- | --- | --- | --- | --- |
| 1 | Signed out, fully closed. Launch the app | Sign-in screen, no crash | | | |
| 2 | Signed in, fully closed. Launch the app | Home with meetups, no crash | | | |
| 3 | Signed in. Background the app for a minute, return | Same screen; data reloads; no sign-in prompt | | | |
| 4 | Signed in. Background long enough for the session to need refreshing (or force an expired token), return | Session refreshes and lists load without a sign-in prompt | | | |
| 5 | Signed out. Sign in with Google | Browser sheet appears; returns to the app signed in | | | |
| 6 | Signed out. Sign in with Apple | Apple sheet appears over the app; returns signed in | | | |
| 7 | App in the background. Tap a meetup push notification | Opens that meetup's group | | | n/a |
| 8 | App fully closed (swipe away). Tap a meetup push notification | App launches and opens that meetup's group | | | n/a |
| 9 | Make the Api unreachable (airplane mode, or point at a dead URL), launch, choose **Quit** on "Service Unavailable" | App closes; no hang | | | |
| 10 | iPad: try to open a second window from the app switcher / multitasking menu | No second LoopMeet window is created | n/a | | n/a |
| 11 | Mac: Window menu → New Window (if present) | No second window | n/a | n/a | |
| 12 | Update over the previous release (install the old build, sign in, then install this build) | Opens signed in without re-authenticating | | | |

Push rows 7–8 need a real device with notifications allowed. The simulator can receive simulated pushes via `xcrun simctl push`, but OneSignal-delivered pushes need a device.

## Android regression

Run rows 1–5 and 7–9 on one Android device or emulator (`deploy/deploy-android.sh -c Debug`). Behavior must match the previous release (FR-007).

## If something fails

- **Crash at launch**: check that both `Info.plist` files contain `UIApplicationSceneManifest` with `__MAUI_DEFAULT_SCENE_CONFIGURATION__` and `SceneDelegate`, and that the class is `[Register("SceneDelegate")]`. A mismatch between the plist name and the registration crashes at launch.
- **Google sign-in returns to a blank app**: confirm `Microsoft.Maui.Controls` restored as 10.0.120 (10.0.110 or later forwards scene URL callbacks to `WebAuthenticator`).
- **Notification tap on cold start does nothing**: OneSignal handles taps through its own notification delegate; capture the device log around the tap and check `OneSignalNotificationTapSource` logging before changing lifecycle code.
