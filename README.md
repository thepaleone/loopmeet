# Meetloop

This is a social app designed to help friends gather.

## Deploy staging client to device

### Connect via `adb` (wireless debugging)

On the device, open **Settings → Developer options → Wireless debugging**. It shows two different addresses: one for pairing (under **Pair device with pairing code**) and one for connecting (on the main Wireless debugging screen).

```bash
# This may be needed if another debugging session had already started.
adb kill-server
# Pair once per device, using the IP:PORT and code from "Pair device with pairing code".
adb pair IP:PAIRING_PORT CODE
# Connect, using the IP:PORT from the main Wireless debugging screen (a different port).
adb connect IP:PORT
# Confirm the device is listed as "device" (not "offline" or "unauthorized").
adb devices -l
```

### Build and run with `dotnet build`

```bash
dotnet build -c Staging -t:Run -f net10.0-android src/LoopMeet.App
```

`-t:Run` builds the app, installs it on the connected device, and launches it.
