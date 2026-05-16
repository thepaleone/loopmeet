#!/bin/zsh
# Deploys the LoopMeet Android app in Release configuration to a connected
# device or emulator.
#
# Usage:
#   ./deploy/deploy-android.sh                 # uses the only connected device
#   DEVICE=adb-XYZ ./deploy/deploy-android.sh  # targets a specific adb serial
#
# Run `adb devices -l` to list connected devices and copy the serial.
#
# AOT and trimming are disabled here as a temporary workaround for the .NET-
# for-Android Release-mode UnsatisfiedLinkError on MauiApplication.n_onCreate.
# Startup is slower but reliable. Remove these flags once the trimmer reliably
# preserves [Register]/JNI-reachable methods.

set -euo pipefail

DEVICE_ARG=()
if [[ -n "${DEVICE:-}" ]]; then
  DEVICE_ARG=("-p:Device=${DEVICE}")
fi

dotnet build -c Release \
  -t:Run -f net10.0-android \
  -p:PublishTrimmed=false \
  -p:AndroidLinkMode=none \
  -p:RunAOTCompilation=false \
  -p:AndroidPackageFormat=apk \
  "${DEVICE_ARG[@]}" \
  src/LoopMeet.App
