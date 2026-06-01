#!/bin/zsh
# Deploys the LoopMeet Android app to a connected device or emulator.
#
# Usage:
#   ./deploy/deploy-android.sh                            # defaults: -c Release, default device
#   ./deploy/deploy-android.sh -d emulator-5554           # explicit adb serial
#   ./deploy/deploy-android.sh -c Debug                   # Debug build
#   ./deploy/deploy-android.sh -c Debug -d emulator-5554  # both overrides
#
# Run `adb devices -l` to list connected devices and copy the serial.
#
# AOT and trimming are disabled here as a temporary workaround for the .NET-
# for-Android Release-mode UnsatisfiedLinkError on MauiApplication.n_onCreate.
# Startup is slower but reliable. Remove these flags once the trimmer reliably
# preserves [Register]/JNI-reachable methods.

set -euo pipefail

DEVICE="adb-4A301FDAS002ED-5gZyL4._adb-tls-connect._tcp"
CONFIGURATION="Release"

usage() {
  echo "Usage: $0 [-d <adb-device-serial>] [-c <Debug|Release>]" >&2
  exit 1
}

while getopts ":d:c:h" opt; do
  case "$opt" in
    d) DEVICE="$OPTARG" ;;
    c) CONFIGURATION="$OPTARG" ;;
    h) usage ;;
    :) echo "Option -$OPTARG requires an argument." >&2; usage ;;
    \?) echo "Unknown option: -$OPTARG" >&2; usage ;;
  esac
done

echo "Deploying configuration=$CONFIGURATION device=$DEVICE"

dotnet clean src/LoopMeet.App

dotnet build -c "$CONFIGURATION" \
-t:Run -f net10.0-android \
-p:PublishTrimmed=false \
-p:AndroidLinkMode=none \
-p:RunAOTCompilation=false \
-p:AndroidPackageFormat=apk \
-p:Device="$DEVICE" \
src/LoopMeet.App
