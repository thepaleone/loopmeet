#!/bin/zsh
# Builds a signed Android App Bundle (.aab) of LoopMeet for upload to Google Play.
#
# Usage:
#   ./deploy/build-android-release.sh -b <build-number>
#
# -b sets ApplicationVersion: the Android versionCode (and, later, the iOS
# CFBundleVersion). Google Play rejects an upload whose number isn't higher
# than every build uploaded before, so use the next unused integer.
#
# The upload key is read from the environment, so no secret is typed on the
# command line or stored in the repo:
#   LOOPMEET_ANDROID_KEYSTORE       path to the upload keystore
#   LOOPMEET_ANDROID_KEY_ALIAS      alias of the upload key inside it
#   LOOPMEET_ANDROID_KEYSTORE_PASS  keystore password
#   LOOPMEET_ANDROID_KEY_PASS       key password (optional; defaults to the
#                                   keystore password, which keytool reuses
#                                   unless told otherwise)
#
# AOT and trimming are disabled for the same reason as deploy-android.sh: the
# Release-mode UnsatisfiedLinkError on MauiApplication.n_onCreate.

set -euo pipefail

SCRIPT="$0"
BUILD_NUMBER=""

usage() {
  echo "Usage: $SCRIPT -b <build-number>" >&2
  exit 1
}

while getopts ":b:h" opt; do
  case "$opt" in
    b) BUILD_NUMBER="$OPTARG" ;;
    h) usage ;;
    :) echo "Option -$OPTARG requires an argument." >&2; usage ;;
    \?) echo "Unknown option: -$OPTARG" >&2; usage ;;
  esac
done

if [[ ! "$BUILD_NUMBER" =~ '^[1-9][0-9]*$' ]]; then
  echo "-b must be a positive integer build number." >&2
  usage
fi

for var in LOOPMEET_ANDROID_KEYSTORE LOOPMEET_ANDROID_KEY_ALIAS LOOPMEET_ANDROID_KEYSTORE_PASS; do
  if [[ -z "${(P)var:-}" ]]; then
    echo "$var is not set." >&2
    exit 1
  fi
done

if [[ ! -f "$LOOPMEET_ANDROID_KEYSTORE" ]]; then
  echo "Keystore not found: $LOOPMEET_ANDROID_KEYSTORE" >&2
  exit 1
fi

export LOOPMEET_ANDROID_KEY_PASS="${LOOPMEET_ANDROID_KEY_PASS:-$LOOPMEET_ANDROID_KEYSTORE_PASS}"

cd "${0:A:h}/.."

# Without it the build succeeds but push notifications never register.
if [[ ! -f src/LoopMeet.App/Platforms/Android/google-services.json ]]; then
  echo "src/LoopMeet.App/Platforms/Android/google-services.json is missing." >&2
  exit 1
fi

OUTPUT_DIR="artifacts/android/$BUILD_NUMBER"

echo "Building release bundle build=$BUILD_NUMBER output=$OUTPUT_DIR"

dotnet clean src/LoopMeet.App -c Release -f net10.0-android

dotnet publish src/LoopMeet.App -c Release -f net10.0-android \
-p:ApplicationVersion="$BUILD_NUMBER" \
-p:AndroidPackageFormat=aab \
-p:PublishTrimmed=false \
-p:AndroidLinkMode=none \
-p:RunAOTCompilation=false \
-p:AndroidKeyStore=true \
-p:AndroidSigningKeyStore="$LOOPMEET_ANDROID_KEYSTORE" \
-p:AndroidSigningKeyAlias="$LOOPMEET_ANDROID_KEY_ALIAS" \
-p:AndroidSigningStorePass=env:LOOPMEET_ANDROID_KEYSTORE_PASS \
-p:AndroidSigningKeyPass=env:LOOPMEET_ANDROID_KEY_PASS \
-o "$OUTPUT_DIR"

echo "Upload this bundle to Google Play:"
ls "$OUTPUT_DIR"/*-Signed.aab
