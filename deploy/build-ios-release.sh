#!/bin/zsh
# Builds an App Store-signed .ipa of LoopMeet for upload to App Store Connect
# (TestFlight). Upload it with Transporter or xcrun altool (see
# specs/021-app-store-release/quickstart.md); this script doesn't upload.
#
# Usage:
#   ./deploy/build-ios-release.sh -b <build-number>
#
# -b sets ApplicationVersion, the iOS CFBundleVersion. App Store Connect accepts
# each version and build pair once, so use the next unused integer. iOS build
# numbers are counted separately from Android's.
#
# Signing comes from the environment, so nothing machine-specific is in the repo:
#   LOOPMEET_IOS_SIGNING_IDENTITY      SHA-1 of the Apple Distribution identity, from
#                                      `security find-identity -v -p codesigning`.
#                                      Not the certificate's name: two certificates can
#                                      share one, and a name would pick either.
#   LOOPMEET_IOS_PROVISIONING_PROFILE  path to the App Store provisioning profile for
#                                      io.loopmeet.app. It must include that identity.
# Neither is a secret: the private key stays in the keychain.
#
# Unlike the Android release script, AOT and trimming keep their defaults: iOS has
# no JIT, so device builds must be AOT, and Release trims only the SDK assemblies.
#
# Contract: specs/021-app-store-release/contracts/build-ios-release.md

set -euo pipefail

SCRIPT="$0"
BUILD_NUMBER=""

usage() {
  echo "Usage: $SCRIPT -b <build-number>" >&2
  exit 1
}

fail() {
  echo "$1" >&2
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

# 1. Build number.
if [[ ! "$BUILD_NUMBER" =~ '^[1-9][0-9]*$' ]]; then
  echo "-b must be a positive integer build number with no leading zero." >&2
  usage
fi

# 2. Both variables.
for var in LOOPMEET_IOS_SIGNING_IDENTITY LOOPMEET_IOS_PROVISIONING_PROFILE; do
  [[ -n "${(P)var:-}" ]] || fail "$var is not set."
done

# 3. The identity is a SHA-1, not a name.
IDENTITY="${LOOPMEET_IOS_SIGNING_IDENTITY:u}"
if [[ ! "$IDENTITY" =~ '^[0-9A-Fa-f]{40}$' ]]; then
  fail "LOOPMEET_IOS_SIGNING_IDENTITY must be the identity's 40-character hexadecimal SHA-1, not its name. Find it with: security find-identity -v -p codesigning"
fi

# 4. The identity is valid in the keychain.
if ! security find-identity -v -p codesigning | grep -qi "$IDENTITY"; then
  fail "Signing identity $IDENTITY is not a valid codesigning identity in the keychain: it is missing, expired or revoked."
fi

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

# 5. The profile exists and decodes.
PROFILE="${LOOPMEET_IOS_PROVISIONING_PROFILE:A}"
PROFILE_PLIST="$WORK_DIR/profile.plist"
if [[ ! -f "$PROFILE" ]] || ! security cms -D -i "$PROFILE" > "$PROFILE_PLIST" 2>/dev/null; then
  fail "Provisioning profile not found or not readable: $LOOPMEET_IOS_PROVISIONING_PROFILE"
fi

profile_value() {
  plutil -extract "$1" raw -o - "$PROFILE_PLIST" 2>/dev/null
}

# 6. The profile is for io.loopmeet.app (<team id>.io.loopmeet.app).
APP_ID="$(profile_value Entitlements.application-identifier || true)"
if [[ "$APP_ID" != *.io.loopmeet.app ]]; then
  fail "The provisioning profile is for App ID '$APP_ID', not <team>.io.loopmeet.app."
fi

# 7. The profile is for App Store distribution: no device list, production push.
if profile_value ProvisionedDevices > /dev/null; then
  fail "The provisioning profile lists devices, so it is a development or ad hoc profile. Use an App Store Connect distribution profile."
fi
PUSH_ENVIRONMENT="$(profile_value Entitlements.aps-environment || true)"
if [[ "$PUSH_ENVIRONMENT" != production ]]; then
  fail "The provisioning profile's aps-environment is '$PUSH_ENVIRONMENT', not production. Regenerate it with Push Notifications enabled on the App ID."
fi

# 8. The profile hasn't expired. ISO 8601 UTC timestamps compare as strings.
EXPIRES="$(profile_value ExpirationDate || true)"
if [[ "$EXPIRES" < "$(date -u +%Y-%m-%dT%H:%M:%SZ)" ]]; then
  fail "The provisioning profile expired on $EXPIRES."
fi

# 9. The profile includes the identity's certificate.
CERTIFICATE_COUNT="$(profile_value DeveloperCertificates || echo 0)"
CERTIFICATE_FOUND=false
for (( i = 0; i < CERTIFICATE_COUNT; i++ )); do
  FINGERPRINT="$(profile_value "DeveloperCertificates.$i" | base64 -D \
    | openssl x509 -inform DER -noout -fingerprint -sha1 2>/dev/null || true)"
  FINGERPRINT="${${FINGERPRINT#*=}//:/}"
  if [[ "${FINGERPRINT:u}" == "$IDENTITY" ]]; then
    CERTIFICATE_FOUND=true
    break
  fi
done
if [[ "$CERTIFICATE_FOUND" != true ]]; then
  fail "The provisioning profile doesn't include the certificate of signing identity $IDENTITY. Regenerate the profile with that certificate."
fi

# Install the profile where the build looks for it. Xcode 16 and later read the
# UserData folder; older tools read MobileDevice. The UUID file name makes this
# safe to repeat.
PROFILE_UUID="$(profile_value UUID)"
for dir in "$HOME/Library/MobileDevice/Provisioning Profiles" \
           "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"; do
  mkdir -p "$dir"
  cp "$PROFILE" "$dir/$PROFILE_UUID.mobileprovision"
done

cd "${0:A:h}/.."

OUTPUT_DIR="artifacts/ios/$BUILD_NUMBER"
DISPLAY_VERSION="$(sed -n 's:.*<ApplicationDisplayVersion>\(.*\)</ApplicationDisplayVersion>.*:\1:p' src/LoopMeet.App/LoopMeet.App.csproj)"

echo "Building App Store ipa version=$DISPLAY_VERSION build=$BUILD_NUMBER profile=$PROFILE_UUID output=$OUTPUT_DIR"

# dotnet clean never restores, and an IDE restore leaves project.assets.json without the
# net10.0-ios/ios-arm64 target (NETSDK1047), so restore for the device runtime first.
dotnet restore src/LoopMeet.App -r ios-arm64
dotnet clean src/LoopMeet.App -c Release -f net10.0-ios -r ios-arm64

dotnet publish src/LoopMeet.App -c Release -f net10.0-ios -r ios-arm64 \
-p:ApplicationVersion="$BUILD_NUMBER" \
-p:CodesignKey="$LOOPMEET_IOS_SIGNING_IDENTITY" \
-p:CodesignProvision="$PROFILE_UUID" \
-o "$OUTPUT_DIR"

IPAS=( "$OUTPUT_DIR"/*.ipa(N) )
(( ${#IPAS} == 1 )) || fail "Expected one .ipa in $OUTPUT_DIR, found ${#IPAS}."
IPA="${IPAS[1]}"

# Post-build checks: read what was actually built and signed.
unzip -q "$IPA" -d "$WORK_DIR/ipa"
APPS=( "$WORK_DIR"/ipa/Payload/*.app(N) )
(( ${#APPS} == 1 )) || fail "Expected one .app in $IPA, found ${#APPS}."
APP="${APPS[1]}"

BUILT_BUILD="$(plutil -extract CFBundleVersion raw -o - "$APP/Info.plist")"
[[ "$BUILT_BUILD" == "$BUILD_NUMBER" ]] \
  || fail "The ipa's CFBundleVersion is $BUILT_BUILD, expected $BUILD_NUMBER."

BUILT_VERSION="$(plutil -extract CFBundleShortVersionString raw -o - "$APP/Info.plist")"
[[ "$BUILT_VERSION" == "$DISPLAY_VERSION" ]] \
  || fail "The ipa's CFBundleShortVersionString is $BUILT_VERSION, expected $DISPLAY_VERSION."

codesign -d --entitlements - --xml "$APP" > "$WORK_DIR/entitlements.plist" 2>/dev/null
SIGNED_PUSH="$(plutil -extract aps-environment raw -o - "$WORK_DIR/entitlements.plist" 2>/dev/null || true)"
[[ "$SIGNED_PUSH" == production ]] \
  || fail "The ipa is signed with aps-environment '$SIGNED_PUSH', expected production."

echo "Verified: version $BUILT_VERSION ($BUILT_BUILD), aps-environment $SIGNED_PUSH."
echo "Upload this ipa to App Store Connect:"
echo "$IPA"
