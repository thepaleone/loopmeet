# Quickstart: Profile Tab and Profile Management

**Date**: 2026-02-26

## Purpose

Validate the complete Profile-tab feature scope:
- `Profile` tab appears last in signed-in tab bar
- profile page displays profile summary (`displayName`, avatar, `User since`, `groupCount`)
- profile edits for display name/avatar
- dedicated password-change modal flow
- social-avatar bootstrap behavior respecting override precedence

## Prerequisites

- .NET 10 SDK
- MAUI build prerequisites for target platform (Android/iOS/MacCatalyst)
- API/Supabase configuration used by local LoopMeet development

## Build and Run

### API

```bash
dotnet restore
dotnet run --project /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.Api
```

### App

```bash
dotnet restore
dotnet build /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.App
```

Run the app from your IDE/simulator/device target.

## Android Build Notes

### Minimal stable Android run/build command

```bash
dotnet build -t:Run -f net10.0-android \
  -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home \
  /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.App/LoopMeet.App.csproj
```

### Full VS Code MAUI Android command (known working)

```bash
dotnet build -t:Run -p:Configuration=Debug -f net10.0-android -p:AdbTarget=-s%20emulator-5554 -p:AndroidAttachDebugger=true -p:AndroidSdbTargetPort=50942 -p:AndroidSdbHostPort=50942 -p:CustomAfterMicrosoftCSharpTargets=/Users/joel/.vscode/extensions/ms-dotnettools.dotnet-maui-1.13.20-darwin-arm64/dist/resources/Custom.After.Microsoft.CSharp.targets -p:MauiVSCodeBuildOutputFile=/var/folders/4w/2_3x4c755zs8w24j396zfg580000gn/T/dotnet-maui/maui-vsc-88d164bf-14f8-4fa0-afd8-ea6bf8d1428c.json -p:AndroidSdkDirectory=/Users/joel/Library/Android/sdk -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home -p:XamlTools=/Users/joel/.vscode/extensions/ms-dotnettools.csharp-2.120.3-darwin-arm64/.xamlTools -p:EnableDiagnostics=True -p:EnableMauiXamlDiagnostics=True /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.App/LoopMeet.App.csproj
```

### Why this matters

- If `JavaSdkDirectory` points at `/Library/Java/JavaVirtualMachines/microsoft-17.jdk/` (without `/Contents/Home`), Android builds may hang around `_CompileResources`.
- Always use `/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home` for this repo’s Android builds.

## Manual Validation Checklist

1. Authenticate and confirm the signed-in tabs are `Home`, `Groups`, `Invitations`, `Profile` in that order.
2. Open `Profile` and confirm it shows:
   - display name
   - avatar or avatar placeholder
   - `User since` date
   - group membership count
3. Change display name and save; confirm success feedback and persisted update after reload.
4. Change avatar and save; confirm updated avatar is displayed.
5. Trigger Change Password and confirm a popup modal opens.
6. Confirm no password entry fields are visible inline on the profile page.
7. Submit invalid password data and confirm modal remains open with actionable error.
8. Submit valid password change and confirm success feedback + modal close with profile context preserved.
9. Validate social-avatar bootstrap behavior:
   - new social-login profile copies avatar when no override exists
   - existing avatar override remains unchanged after later social sign-in/profile bootstrap
10. Validate edge cases:
   - no avatar
   - no groups (`groupCount=0`)
   - profile/API failure responses show recoverable feedback

## Automated Tests (Required)

Run existing and updated suites covering changed behavior:

```bash
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.App.Tests
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Api.Tests
```

Add/adjust tests for:
- profile tab route/order and profile screen state logic
- profile read/update contract behavior
- avatar precedence and social-avatar bootstrap rules
- password-change modal request/response handling

## Quality Rule for This Feature

If functionality is added or changed, matching automated tests must be added or updated in the same change set before merge.

## Execution Evidence (2026-02-27)

- Automated suite run:
  - `dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Api.Tests/LoopMeet.Api.Tests.csproj`
  - Result: Passed (`19` passed, `0` failed)
- Automated suite run:
  - `dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`
  - Result: Passed (`5` passed, `0` failed)

## Completed Acceptance Coverage

- Profile tab appears in signed-in tab bar after Invitations and opens profile summary view.
- Profile summary projection returns display name, avatar source/avatar url, user-since date, and group count.
- Profile patch flow updates display name and avatar override and preserves override precedence.
- Password-change endpoint validates mismatch/current-password rules and returns expected status codes.
- Social-login avatar bootstrap copies avatar when no override exists and preserves existing override when present.
