#!/bin/zsh
# Deploys the LoopMeet Android app in Release configuration to a connected
# device or emulator.
#
# Usage:
#   ./deploy/deploy-android.sh                # uses the default physical device
#   ./deploy/deploy-android.sh emulator-5554  # targets an explicit adb serial
#
# Run `adb devices -l` to list connected devices and copy the serial.
#
# AOT and trimming are disabled here as a temporary workaround for the .NET-
# for-Android Release-mode UnsatisfiedLinkError on MauiApplication.n_onCreate.
# Startup is slower but reliable. Remove these flags once the trimmer reliably
# preserves [Register]/JNI-reachable methods.

set -euo pipefail

# First positional argument overrides the default; falls back to the physical
# device serial when no arg is given. Use `emulator-5554` (or similar) to
# target a running emulator.
DEVICE="${1:-adb-4A301FDAS002ED-5gZyL4._adb-tls-connect._tcp}"

dotnet build -c Release \
  -t:Run -f net10.0-android \
  -p:PublishTrimmed=false \
  -p:AndroidLinkMode=none \
  -p:RunAOTCompilation=false \
  -p:AndroidPackageFormat=apk \
  -p:Device="$DEVICE" \
  src/LoopMeet.App
