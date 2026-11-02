# Quickstart: First Google Play Internal Testing Release

## Build the bundle

```bash
export LOOPMEET_ANDROID_KEYSTORE=~/path/to/loopmeet-upload.keystore
export LOOPMEET_ANDROID_KEY_ALIAS=loopmeet-upload
export LOOPMEET_ANDROID_KEYSTORE_PASS='…'   # from your password manager
./deploy/build-android-release.sh -b 1
```

Upload `artifacts/android/1/io.loopmeet.app-Signed.aab`. Use the next unused number for every later upload.

## Device checks (install from the Play internal testing link)

| # | Check | Expected | Result |
|---|-------|----------|--------|
| 1 | Launch the Play-installed app (cold start) | Opens to sign-in or Home, no crash | |
| 2 | Sign in with Google | Signed in, Home loads groups and meetups | |
| 3 | Profile → change photo → Choose from library (Android 13+) | System photo picker, no permission prompt; avatar uploads | |
| 4 | Profile → change photo → Take a new photo | Camera opens; avatar uploads | |
| 5 | Create meetup → search location with permission granted | Nearby results | |
| 6 | Receive a push for a new meetup; tap it with the app closed | App opens to the meetup | |
| 7 | Settings → Apps → LoopMeet → version | `0.1.0` | |
