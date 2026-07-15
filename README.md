# 2UP client (walking skeleton)

Unity 6 (6000.0.75f1) client for **2UP**, an online 2-player minigame app. Android-first, portrait, flat UGUI. Speaks binary protobuf over a WebSocket to the Go server.

Scenes: `Boot` (device id + connect + ClientHello) → `Lobby` (quick match / create room / join by code) → `ConnectFour` (7x6 board, server-authoritative, rematch). See [CLAUDE.md](CLAUDE.md) for the architecture rules.

## Pointing at a server

All endpoint URLs live in **`Assets/Config/ServerConfig.asset`** (select it in the Project window):

- **Active Environment** — picks the `Dev` or `Staging` slot.
- **Dev Url** — default `ws://10.0.2.2:8080/ws`. `10.0.2.2` is the host machine's localhost as seen from the **Android emulator**. For a physical device on the same Wi-Fi, replace it with your PC's LAN IP (e.g. `ws://192.168.1.20:8080/ws`). For in-editor Play mode, use `ws://localhost:8080/ws` (`10.0.2.2` does not resolve on desktop).
- **Staging Url** — empty; fill in when a staging server exists (`wss://…`).

URLs are never hardcoded in logic; `NetworkClient` reads `ServerConfig.ActiveUrl`.

## Regenerating protobuf C#

The contract is `proto/twoup.proto` (v0 — do not modify; see CLAUDE.md). To regenerate after pulling contract changes:

```powershell
.\tools\generate-protos.ps1
```

The script downloads `protoc` (from the `Google.Protobuf.Tools` NuGet package) into `tools/protoc/` if missing and writes `Assets/Scripts/Generated/Twoup.cs`. The runtime is vendored in `Assets/Plugins/Protobuf/` (Google.Protobuf 3.35.1 + System.Memory/System.Buffers/System.Runtime.CompilerServices.Unsafe). Keep protoc and the runtime DLL on the same major.minor.

## Rebuilding scenes / config

Scenes and the ServerConfig asset are authored by an editor script, not by hand:

- Editor menu: **2UP → Build All** (idempotent; overwrites the three scenes)
- CLI: `Unity.exe -batchmode -quit -projectPath . -executeMethod TwoUp.EditorTools.SkeletonBuilder.BuildAll`

## Building the APK

- Editor menu: **2UP → Build Android APK** (output: `Builds/twoup-client.apk`)
- CLI: `Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod TwoUp.EditorTools.SkeletonBuilder.BuildApk`

Requires Android Build Support (SDK/NDK/OpenJDK) installed via Unity Hub.

## Trying a full match

1. Start the Go server (default `:8080`).
2. Device A: install the APK on an emulator (dev URL `ws://10.0.2.2:8080/ws` works out of the box) or a phone (set LAN IP first, rebuild).
3. Device B: second device, or press Play in the editor on the `Boot` scene (set Dev Url to `ws://localhost:8080/ws`).
4. Both tap **Quick Match** (or Create Room on one, Join Room with the code on the other), play Connect Four, then **Rematch** from the game-over panel.

Always enter Play mode from `Assets/Scenes/Boot.unity` — the persistent App object (NetworkClient + AppStateMachine) only exists there.

## Known TODOs

- `TODO(contract)`: no message to cancel a created room or leave/forfeit a match; rematch decline is implicit (contract v0 is intentionally untouched).
- Row-0 orientation of `ConnectFourState.cells` is assumed to be the bottom row — flip `RowZeroIsBottom` in `ConnectFourController` if the server differs.
- `ws://` (cleartext) is for dev; move to `wss://` for anything public.
