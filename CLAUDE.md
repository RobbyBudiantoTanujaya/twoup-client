# 2UP client — architecture rules

Unity 6 (6000.0.75f1) client for "2UP", an online 2-player minigame app. Android-first, portrait, flat 2D UI (UGUI + TextMeshPro). Walking skeleton: Boot → Lobby → ConnectFour.

## Hard rules

1. **Protobuf contract is managed via `docs/TDD.md` section 3.1.** It lives in `proto/twoup.proto`. C# is generated with `protoc` into `Assets/Scripts/Generated/` (regenerate with `tools/generate-protos.ps1`; runtime is a vendored `Google.Protobuf.dll` in `Assets/Plugins/Protobuf/`). **Any change to the proto MUST follow `docs/TDD.md` section 3.1 exactly (additive only)**, and the client and server `.proto` files must stay identical. Outside of that, if something is missing, add a `// TODO(contract):` comment where you needed it instead.
2. **Networking is isolated in `NetworkClient`** (`Assets/Scripts/Net/NetworkClient.cs`, built on the NativeWebSocket package). It owns the socket: connect, send `Envelope`, parse received `Envelope`s and dispatch payloads as C# events. **Game/UI code never touches the socket or NativeWebSocket types directly** — it calls `NetworkClient.Instance.Send(...)` and subscribes to its typed events.
3. **Server-authoritative.** The client sends `GameInput` and renders whatever `GameState` the server pushes. No local game rules beyond input legality *hints* (e.g. disabling a full column's button). Never predict or mutate board state locally.
4. **Scene flow is driven by `AppStateMachine`** (thin, on the persistent `App` object created in Boot): Boot → Lobby → InGame → Result/Rematch → Lobby. Scenes call `AppStateMachine.Instance.To*()`; nothing else loads scenes.

## Conventions

- Endpoint URLs come **only** from the `ServerConfig` ScriptableObject (`Assets/Config/ServerConfig.asset`, dev/staging slots). Never hardcode a URL in logic.
- The persistent `App` GameObject (NetworkClient + AppStateMachine, `DontDestroyOnLoad`) exists only in the Boot scene — always enter Play from `Assets/Scenes/Boot.unity`.
- Services (`NetworkClient`, `AppStateMachine`) are accessed via their `Instance` singletons across scenes; UI elements are wired through serialized fields at authoring time — no runtime `Find()`.
- Scenes are authored by `Assets/Editor/SkeletonBuilder.cs` (menu: `2UP/Build All`, or batchmode `-executeMethod TwoUp.EditorTools.SkeletonBuilder.BuildAll`). Rebuilding scenes overwrites them — prefer editing the builder over hand-editing scenes so the setup stays reproducible. Never hand-edit `.unity`/`.prefab` YAML.
- One MonoBehaviour/ScriptableObject class per `.cs` file, file name = class name. Commit `.meta` files together with their assets.
- Direction note (not current state): scene authoring is moving to per-scene builder scripts under `Assets/Editor/SceneBuilders/` (still menu `2UP/Build All`), and `Lobby.unity` is slated for retirement, replaced by `Home`/`InviteRoom`/`Queue` screens as the MVP scene set lands.

## Scope (MVP)

2UP ships 6 games (`connect_four`, `reflex_duel`, `air_hockey`, `wall_defense`, `keepup_duo`, `battleship`) across the 14 screens defined in `docs/TDD.md` section 5, with voting, rematch, invite, async play, and reconnect per `docs/TDD.md`. Monetization goes through a stub provider (SDKs to follow behind the `TWOUP_FIREBASE`, `TWOUP_ADS`, and `TWOUP_IAP` scripting defines). English-only, Android-only. Still no localization and no art pass — function over form until design handoff.
