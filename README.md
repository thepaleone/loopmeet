# Meetloop

This is a social app designed to help friends gather.

## Deploy staging client to device

### Connect via `adb`

```bash
# This may be needed if another debugging session had already started.
adb kill-server
# Change the IP, port and code to those displayed on your device
adb pair IP:PORT CODE
```

### Deploy via `dotnet publish`

```bash
dotnet build -c Staging -t:Run -f net10.0-android src/LoopMeet.App
```
