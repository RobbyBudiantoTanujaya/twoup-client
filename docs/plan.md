# Plan: 2UP client MVP — dari walking skeleton ke 6 game + meta (untuk ccq)

Sumber: `docs/TDD.md` v1.1 (2026-07-16) + `docs/GDD.md` v1.1. Kedua file ada di repo ini — task boleh (dan diharapkan) membacanya untuk detail kontrak; bagian yang kritis tetap disalin inline di tiap task.

**CAKUPAN: client Unity SAJA.** Semua pekerjaan server Go (Pairing actor, 5 game baru, store extension, FCM sender, sweep, endpoint `/r/{code}/status`) ada di TDD §4 dan BUKAN bagian plan ini — client dibangun terhadap kontrak proto TDD §3.1 dan bisa diuji headless tanpa server.

**CATATAN UNTUK INGEST:** SEMUA task di plan ini `live_editor: false` BY DESIGN — jangan dikonversi. Alasan di Global decisions #2.

---

## Global decisions (dikunci sekarang, worker tidak memutuskan ulang)

1. **Pisah lapisan.** Logika = C# murni (plain class/static, boleh tipe math Unity), diuji EditMode headless, paralel. Visual = scene `.unity` ASLI yang terlihat & bisa diedit di edit mode — TIDAK ada UI yang dibangun runtime.
2. **Cara authoring visual (PENGECUALIAN eksplisit dari default ccq "jangan sentuh .unity/.meta"):** repo ini punya konvensi terkunci (CLAUDE.md + terbukti di walking skeleton): scene digenerate oleh **editor builder script** (`Assets/Editor/SceneBuilders/*.cs`) yang dijalankan via batchmode `-executeMethod`, lalu file `.unity` + `.meta` hasilnya DI-COMMIT. Worker TIDAK pernah hand-edit YAML scene. Verifikasi hierarki = EditMode test yang `EditorSceneManager.OpenScene()` + assert objek/komponen/referensi (mekanis, jalan headless). Aturan konflik: **satu scene = satu owner task; semua task visual `parallel_safe: false`** (berbagi `EditorBuildSettings` + file builder bersama).
3. **Assembly:** `TwoUp.Runtime.asmdef` (Assets/Scripts, rootNamespace `TwoUp`), `TwoUp.Editor.asmdef` (Assets/Editor), `TwoUp.Tests.EditMode.asmdef` (Assets/Tests/EditMode, test assembly, boleh pakai `UnityEditor`). Namespace baru untuk logika murni: `TwoUp.Logic`.
4. **Kontrak wire:** `proto/twoup.proto` di repo ini di-extend PERSIS mengikuti `docs/TDD.md` §3.1 (murni additive terhadap v0). C# regen via `tools/generate-protos.ps1` (auto-download protoc). Server akan me-mirror file yang sama — jangan improvisasi field/nomor di luar TDD.
5. **Belum ada design handoff** (GDD §10: fase design belum berjalan). Semua UI = function-over-form memakai bahasa visual skeleton (flat panel, `UISprite` sliced untuk tombol, TMP LiberationSans). HUKUM FIDELITY UI (mockup 7-sumbu, sprite chrome per radius) TIDAK berlaku di plan ini dan akan jadi plan re-skin terpisah saat handoff ada. Placeholder yang dikunci: emote = tombol teks TMP (`+1`, `LOL`, `WOW`, `CRY`, `FIRE`, `GG`); game card = panel warna + nama game; tidak ada dependensi art baru.
6. **Tidak ada package baru.** Firebase/AdMob/Unity IAP/Install Referrer = prasyarat manual manusia. Semua kode yang menyentuhnya di belakang scripting define (`TWOUP_FIREBASE`, `TWOUP_ADS`, `TWOUP_IAP`) + interface dengan stub provider, sehingga project SELALU compile tanpa SDK.
7. **Determinisme:** generator/gate memakai seed/clock yang di-inject (`System.Random(seed)`, parameter `float nowSeconds`) — tidak ada `UnityEngine.Random`/`Time.time` di kode logika murni.
8. **Server-authoritative dipertahankan** (CLAUDE.md rule 3): client kirim input, render state; satu-satunya logika lokal = legality hint + placement pre-validation Battleship.
9. **Mapping game → scene (kanonik, dipakai semua task):**
   | game_id | sceneName | displayName | mode | pacing |
   |---|---|---|---|---|
   | connect_four | ConnectFour | Connect Four | Versus | TurnBased |
   | reflex_duel | ReflexDuel | Reflex Duel | Versus | Live |
   | air_hockey | AirHockey | Air Hockey | Versus | Live |
   | wall_defense | WallDefense | Wall Defense | CoOp | Live |
   | keepup_duo | KeepUpDuo | Keep-Up Duo | CoOp | Live |
   | battleship | Battleship | Battleship | Versus | TurnBased |
10. **Keep-Up Duo kontrol = paddle-drag 1D** (TDD Blocker B1 default engineering; bisa direvisi fase design — jangan tunda task karenanya).
11. **Resolusi inkonsistensi TDD §7b:** `VotingCountdownFormatter` memakai **ceiling** (2999ms → "3") — nama test di TDD (`RoundsDown`) salah tulis; expected value-nya (2999→"3") yang benar. Test dinamai `Format_CeilsToWholeSeconds`.
12. **HUKUM SCRIPT:** setiap MonoBehaviour/ScriptableObject = satu file .cs, nama file = nama class (abstract/generic dikecualikan). Ini acceptance criterion di SETIAP task yang membuat script.
13. **Interaksi placement Battleship (MVP):** tray 5 tombol kapal → tap sel grid = taruh origin → `Btn_Rotate` toggle arah → `Btn_Random` pakai generator → `Btn_LockPlacement` kirim. Bukan drag bebas.
14. **Mallet/paddle sendiri di game real-time:** render posisi drag lokal langsung (responsif); posisi dari server dipakai untuk opponent + bola/puck (via `SnapshotInterpolator`); mallet sendiri di-snap ke posisi server hanya kalau menyimpang > 150 unit.

## Manual prerequisites (manusia, BUKAN task ccq)

- **Server Go**: implementasi TDD §4 (plan terpisah di repo `twoup-server`). Client plan ini tidak memblokir dan tidak diblokir olehnya (uji headless tanpa server), tapi E2E baru bisa setelah server lanjut.
- Firebase project + `google-services.json` + Firebase Unity SDK (Messaging) + EDM4U → setelah itu aktifkan define `TWOUP_FIREBASE`.
- Google Mobile Ads Unity plugin + AdMob app/unit id → define `TWOUP_ADS`. Unity IAP (`com.unity.purchasing`) + SKU `premium_unlock` di Play Console → define `TWOUP_IAP`.
- `mainTemplate.gradle` custom + dependency `com.android.installreferrer:installreferrer:2.2` (kode client sudah compile tanpanya; referrer hanya jalan di device build yang menyertakannya).
- Domain landing page final + `assetlinks.json` (TDD Blocker B2) → sampai ada, `ServerConfig.inviteLinkBase` memakai placeholder.
- Art per `docs/asset-list.md` + font display/UI (SH-17/18) — dipakai plan re-skin berikutnya, bukan plan ini.

---

## FASE 0 — Fondasi (priority 1, serial)

## Task: Update CLAUDE.md client — ganti scope guard skeleton dengan scope MVP
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA/batch (docs only). File `CLAUDE.md` di root repo `twoup-client` masih memuat bagian "## Scope guard (walking skeleton)" berbunyi "No art/animation/audio pass, no ads, no IAP, no deep links, no localization, any second game" — ini USANG dan menyesatkan worker berikutnya. Edit file itu:
1. Ganti seluruh bagian "## Scope guard (walking skeleton)" dengan "## Scope (MVP)" berisi: 6 game (connect_four, reflex_duel, air_hockey, wall_defense, keepup_duo, battleship), 14 screen per `docs/TDD.md` §5, voting/rematch/invite/async/reconnect per `docs/TDD.md`, monetisasi via stub provider (SDK menyusul, define `TWOUP_FIREBASE`/`TWOUP_ADS`/`TWOUP_IAP`), English-only, Android-only. Tetap TANPA localization dan tanpa art pass (function-over-form sampai design handoff).
2. Di "Hard rules" poin 1, ganti kalimat "Do not modify the contract..." menjadi: kontrak dikelola lewat `docs/TDD.md` §3.1 — perubahan proto HARUS persis mengikuti TDD (additive), file client & server harus identik; selain itu tetap `TODO(contract)`.
3. Tambah di "Conventions": scene digenerate builder script di `Assets/Editor/SceneBuilders/` (menu 2UP → Build All), `Lobby.unity` dipensiunkan digantikan Home/InviteRoom/Queue (setelah task retirement selesai — tulis sebagai catatan arah, bukan klaim keadaan sekarang).
Jangan ubah bagian lain. Jangan menyentuh kode.
**Acceptance criteria:**
- `CLAUDE.md` tidak lagi memuat string "walking skeleton" pada bagian scope, tidak memuat "no deep links" / "no IAP" / "no second game".
- `CLAUDE.md` memuat referensi eksplisit ke `docs/TDD.md` §3.1 untuk perubahan proto dan menyebut ketiga define `TWOUP_FIREBASE`, `TWOUP_ADS`, `TWOUP_IAP`.
- Tidak ada file selain `CLAUDE.md` yang berubah.

## Task: Struktur asmdef + test scaffolding EditMode
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA/batch. Project saat ini TANPA asmdef dan TANPA test. Buat:
1. `Assets/Scripts/TwoUp.Runtime.asmdef` — name `TwoUp.Runtime`, rootNamespace `TwoUp`, references (by name): `Unity.TextMeshPro`, `UnityEngine.UI`, `NativeWebSocket`. JANGAN set `overrideReferences` (biarkan default supaya `Assets/Plugins/Protobuf/Google.Protobuf.dll` yang auto-referenced tetap ter-link).
2. `Assets/Editor/TwoUp.Editor.asmdef` — name `TwoUp.Editor`, includePlatforms `["Editor"]`, references: `TwoUp.Runtime`, `Unity.TextMeshPro`, `UnityEngine.UI`.
3. `Assets/Tests/EditMode/TwoUp.Tests.EditMode.asmdef` — name `TwoUp.Tests.EditMode`, includePlatforms `["Editor"]`, references: `TwoUp.Runtime`, `TwoUp.Editor`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `defineConstraints: ["UNITY_INCLUDE_TESTS"]`; precompiled references tidak perlu override.
4. Tambah package test framework kalau belum ada di `Packages/manifest.json`: `"com.unity.test-framework": "1.4.5"` (satu-satunya package baru yang DIIZINKAN plan ini; sudah diputuskan di sini, bukan judgment call).
5. Smoke test `Assets/Tests/EditMode/SmokeTests.cs`: class `SmokeTests`, method `[Test] Sanity_AlwaysPasses()` assert `1+1==2`, dan `[Test] ProtoTypes_Exist()` assert `typeof(Twoup.V1.Envelope) != null`.
Semua script existing harus tetap compile (mereka otomatis pindah ke assembly baru). `.meta` setiap asmdef ikut commit.
**Acceptance criteria:**
- File `Assets/Scripts/TwoUp.Runtime.asmdef`, `Assets/Editor/TwoUp.Editor.asmdef`, `Assets/Tests/EditMode/TwoUp.Tests.EditMode.asmdef` ada beserta `.meta`-nya.
- EditMode batchmode pass: `SmokeTests.Sanity_AlwaysPasses` dan `SmokeTests.ProtoTypes_Exist` hijau (baseline verify project).
- Tidak ada error compile di log; tidak ada file `.cs` existing yang isinya berubah (hanya asmdef + test baru + manifest).

## Task: Extend proto kontrak per TDD §3.1 + regen C#
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "Struktur asmdef + test scaffolding EditMode" — cek `Assets/Tests/EditMode/TwoUp.Tests.EditMode.asmdef` ada; kalau belum, berhenti dan laporkan.
Edit `proto/twoup.proto` (root repo): tambahkan SEMUA message/field/enum baru PERSIS seperti tertulis di `docs/TDD.md` §3.1.1 sampai §3.1.7 — baca file itu dan salin verbatim: (a) field baru di `RoomCreated`/`GameResult`/`RematchRequest` + enum `RematchChoice`; (b) entri oneof `Envelope` field 31, 60-67, 70-76, 80-81, 90-101 + message voting/room/async/emote (`PairFound`...`MatchWentAsync`, `StartBotMatch`); (c) per-game payload `ReflexDuelInput/State`, `AirHockeyInput/State`, `WallDefenseInput/State` + `WDBall`, `KeepUpDuoInput/State`, `ShipPlacement`, `BattleshipPlaceInput/FireInput/State` + enum `BattleshipPhase`, `ReflexRoundPhase`; (d) meta §3.1.7 (`GetProfile`...`RegisterPushToken`). ATURAN KERAS: v0 field 1-50 TIDAK diubah satu pun; nomor field mengikuti TDD persis; kalau TDD dan file proto konflik, TDD menang.
Lalu jalankan `powershell -NoProfile -ExecutionPolicy Bypass -File tools/generate-protos.ps1` dan commit `proto/twoup.proto` + `Assets/Scripts/Generated/Twoup.cs` bersama.
Tambah test `Assets/Tests/EditMode/ProtoContractTests.cs` class `ProtoContractTests`:
- `Envelope_HasVotingPayloads()` — construct `new Twoup.V1.Envelope { PairFound = new Twoup.V1.PairFound() }` lalu assert `PayloadCase == PayloadOneofCase.PairFound`; ulangi untuk `VotingLocked`, `StartBotMatch`, `MatchWentAsync`, `GetProfile`, `RegisterPushToken`.
- `ConnectFourContract_Unchanged()` — assert `ConnectFourState` masih punya field `Cells` (`RepeatedField<int>`) dan `NextPlayerId`.
- `BattleshipState_RoundTrips()` — marshal/unmarshal `BattleshipState` dengan `MyFleet` 100 elemen, assert identik.
**Acceptance criteria:**
- `proto/twoup.proto` memuat message `PairFound`, `StartBotMatch`, `MatchWentAsync`, `BattleshipState`, `RegisterPushToken`, `WalletUpdate` (grep nama), dan SEMUA field v0 lama tetap ada dengan nomor tak berubah (grep `client_hello = 1`, `error = 50`).
- `Assets/Scripts/Generated/Twoup.cs` ter-regenerate (memuat `class PairFound`, `class BattleshipState`) dan compile.
- EditMode test `ProtoContractTests` (3 method) hijau via batchmode.
- Setiap class MonoBehaviour/ScriptableObject baru (tidak ada di task ini) — n/a; tidak ada TODO/FIXME baru di file yang diubah.
**Verify commands:**
- `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\user\.ccq\bin\unity-guarded.ps1 EditMode -testFilter TwoUp.Tests.EditMode.ProtoContractTests`

---

## FASE 1 — Logika inti (priority 2; paralel kecuali disebut)

## Task: RateGate + extend NetworkClient (event baru, SendRateLimited, ping interval)
**Priority:** 2
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "Extend proto kontrak per TDD §3.1 + regen C#" — cek `Twoup.V1.PairFound` ada di `Assets/Scripts/Generated/Twoup.cs`; kalau belum, berhenti dan laporkan.
1. Buat `Assets/Scripts/Logic/RateGate.cs` — plain class, namespace `TwoUp.Logic`, TANPA UnityEngine kecuali tidak perlu sama sekali:
```csharp
public class RateGate
{
    public RateGate(float minIntervalSeconds);
    /// true = boleh lewat sekarang (mencatat nowSeconds); false = masih dalam interval.
    public bool TryPass(string key, float nowSeconds);
}
```
2. Edit `Assets/Scripts/Net/NetworkClient.cs` (namespace `TwoUp.Net`, pola event existing `public event Action<T> XxxReceived` + case di switch `HandleMessage`). Tambah event untuk SEMUA payload server→client baru: `PairFoundReceived(PairFound)`, `VoteUpdateReceived(VoteUpdate)`, `VotingLockedReceived(VotingLocked)`, `VotingShowdownReceived(VotingShowdown)`, `VotingCancelledReceived(VotingCancelled)`, `RematchStatusUpdateReceived(RematchStatusUpdate)`, `RoomJoinPendingReceived(RoomJoinPending)`, `RoomExpiredReceived(RoomExpired)`, `AsyncMatchListReceived(AsyncMatchList)`, `MatchResumedReceived(MatchResumed)`, `MatchWentAsyncReceived(MatchWentAsync)`, `EmoteBroadcastReceived(EmoteBroadcast)`, `ProfileDataReceived(ProfileData)`, `PairDetailReceived(PairDetail)`, `ShopDataReceived(ShopData)`, `WalletUpdateReceived(WalletUpdate)` — semua tipe dari `Twoup.V1`.
3. Ganti `const float PingIntervalSeconds = 15f` menjadi `public float PingIntervalSeconds { get; set; } = 15f;` (scene ReflexDuel akan set 2f saat masuk, 15f saat keluar).
4. Tambah `public void SendRateLimited(Envelope envelope, string key, float minIntervalSeconds)`: leading edge kirim langsung via `RateGate.TryPass(key, Time.unscaledTime)`; kalau tertahan, simpan envelope TERBARU per key di `Dictionary<string, Envelope>` dan flush di `Update()` saat `TryPass` lolos (trailing latest — posisi drag terakhir tidak boleh hilang).
5. Test `Assets/Tests/EditMode/RateGateTests.cs`: `TryPass_FirstCallPasses`, `TryPass_WithinIntervalBlocked` (t=0 pass, t=0.03 dengan interval 0.05 → false), `TryPass_AfterIntervalPasses` (t=0.06 → true), `TryPass_KeysIndependent`.
HUKUM SCRIPT: `RateGate` di file sendiri, nama file = nama class. Dilarang menyentuh file lain.
**Acceptance criteria:**
- `Assets/Scripts/Logic/RateGate.cs` ada; `RateGateTests` (4 method) hijau via batchmode EditMode.
- `NetworkClient.cs` memuat 16 event baru di atas (grep `PairFoundReceived`, `ShopDataReceived`, `MatchWentAsyncReceived`) dan `SendRateLimited` dengan signature persis.
- Tidak ada referensi `NativeWebSocket` atau socket di luar `NetworkClient.cs` (grep `using NativeWebSocket` hanya 1 file).
- EditMode suite penuh pass.

## Task: GameCatalog ScriptableObject + asset seed
**Priority:** 2
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "Struktur asmdef + test scaffolding EditMode".
1. `Assets/Scripts/App/GameCatalog.cs` — ScriptableObject persis TDD §3.3: nested `[System.Serializable] class Entry` dengan field `string gameId; string sceneName; string displayName; GameMode mode; GamePacing pacing; Sprite cardArt;`, enum `GameMode { Versus, CoOp }`, `GamePacing { Live, TurnBased }`, `Entry[] entries`, method `Entry Find(string gameId)` (Array.Find). `[CreateAssetMenu(fileName="GameCatalog", menuName="2UP/Game Catalog")]`.
2. Editor util `Assets/Editor/SceneBuilders/CatalogSeeder.cs` (namespace `TwoUp.EditorTools`, static class): method `[MenuItem("2UP/Create Game Catalog")] public static void CreateOrUpdate()` yang membuat/menimpa `Assets/Config/GameCatalog.asset` berisi 6 entri PERSIS tabel Global decisions #9 (gameId/sceneName/displayName/mode/pacing; cardArt null — art belum ada). Jalankan via batchmode `-executeMethod TwoUp.EditorTools.CatalogSeeder.CreateOrUpdate` dan commit `Assets/Config/GameCatalog.asset` + `.meta`.
3. Test `Assets/Tests/EditMode/GameCatalogTests.cs`: `Find_ReturnsEntryByExactId` (load asset via `AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Config/GameCatalog.asset")`, assert `Find("air_hockey").sceneName == "AirHockey"`), `Find_UnknownIdReturnsNull`, `Entries_HasExactlySixInProductionOrder` (urutan: connect_four, reflex_duel, air_hockey, wall_defense, keepup_duo, battleship).
HUKUM SCRIPT: satu class per file.
**Acceptance criteria:**
- `Assets/Scripts/App/GameCatalog.cs` + `Assets/Config/GameCatalog.asset` (+ kedua `.meta`) ada; asset memuat 6 entri (grep `gameId: battleship` di YAML asset).
- `GameCatalogTests` (3 method) hijau via batchmode EditMode; suite penuh pass.

## Task: Extend MatchContext + AppStateMachine (state & routing baru)
**Priority:** 2
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "GameCatalog ScriptableObject + asset seed" — cek `Assets/Scripts/App/GameCatalog.cs` ada; kalau belum, berhenti dan laporkan.
1. Edit `Assets/Scripts/App/MatchContext.cs` (static class existing, namespace `TwoUp`): tambah field static `string PairId;`, `string PendingRoomCode;`, `bool VsBotMode;`, `Twoup.V1.GameOver LastGameOver;`, `Google.Protobuf.ByteString PendingResumeState;`, `int SeriesWinsMine;`, `int SeriesWinsTheirs;`. `Clear()` existing ikut me-reset SEMUA field baru KECUALI `PendingRoomCode` (deep link harus selamat dari Clear pra-match).
2. Edit `Assets/Scripts/App/AppStateMachine.cs`: enum `State` menjadi `{ Boot, Home, Invite, Queue, Voting, InGame, Result, Profile, Shop, Settings, AsyncList }` — CATATAN: JANGAN hapus method `ToLobby()` dan scene name "Lobby" dulu (masih dipakai BootController sampai task "Retire Lobby"); tambah `[SerializeField] private GameCatalog catalog;` + property `public GameCatalog Catalog => catalog;` dan method baru: `ToHome()`, `ToInvite()`, `ToQueue()`, `ToVoting()`, `ToResult()`, `ToProfile()`, `ToShop()`, `ToSettings()`, `ToAsyncList()` (masing-masing set `Current` + `SceneManager.LoadScene("<NamaScene>")` dengan nama scene = nama state), `ToGame(string gameId)` (lookup `catalog.Find(gameId).sceneName`; null → `Debug.LogError` + tetap di scene), `SetInGame()`/`SetResult()` existing dipertahankan.
3. Test `Assets/Tests/EditMode/MatchContextTests.cs`: `Clear_ResetsMatchFieldsButKeepsPendingRoomCode` (set semua field, Clear, assert PendingRoomCode bertahan dan sisanya default), `Clear_ResetsSeriesCounters`.
HUKUM SCRIPT: satu class per file; tidak membuat file baru selain test.
**Acceptance criteria:**
- `MatchContext.cs` memuat 7 field baru (grep `PendingRoomCode`, `VsBotMode`, `PendingResumeState`, `SeriesWinsMine`); `AppStateMachine.cs` memuat `ToGame(string gameId)` + `ToAsyncList()` + field `catalog`, dan MASIH memuat `ToLobby()`.
- `MatchContextTests` (2 method) hijau via batchmode EditMode; suite penuh pass; compile tanpa error.

## Task: Helper murni A — RoomCodeSanitizer, DeepLinkParser, InstallReferrerParser (+reader wrapper)
**Priority:** 2
**Parallel safe:** true
**Description:**
LOGIKA/batch. Depends on: "Struktur asmdef + test scaffolding EditMode". Semua di namespace `TwoUp.Logic`, folder `Assets/Scripts/Logic/`, static class murni tanpa MonoBehaviour:
1. `RoomCodeSanitizer.cs`: `public static string Sanitize(string raw)` — uppercase, buang semua karakter di luar alfabet `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (alfabet server, tanpa 0/O/1/I), potong maksimal 6 karakter; null/kosong → "".
2. `DeepLinkParser.cs`: `public static string ExtractRoomCode(string url)` — menerima `https://<host-apapun>/r/{code}` ATAU `twoup://r/{code}` (case-insensitive pada skema/host, path `/r/` literal), hasil dilewatkan `RoomCodeSanitizer.Sanitize`; hasil kosong/pola tak cocok → null. Query string/fragmen setelah code diabaikan.
3. `InstallReferrerParser.cs`: `public static string ExtractRoomCode(string referrer)` — referrer = query-string style (`utm_source=invite&utm_content=ABC234`, sudah URL-decoded oleh API); parse manual split `&`/`=`, ambil `utm_content`, sanitize; tidak ada → null.
4. `Assets/Scripts/App/InstallReferrerReader.cs` — MonoBehaviour-less static class namespace `TwoUp`, method `public static void ReadOnce(System.Action<string> onRoomCode)`: kalau `PlayerPrefs.GetInt("twoup.referrer_consumed", 0) == 1` → return tanpa callback; di `#if UNITY_ANDROID && !UNITY_EDITOR` gunakan `AndroidJavaObject` terhadap `com.android.installreferrer.api.InstallReferrerClient` (build → startConnection → getInstallReferrer → referrer string → `InstallReferrerParser.ExtractRoomCode` → callback bila non-null; try/catch semua, kegagalan = diam), di platform lain → langsung set flag consumed. Setelah membaca (sukses/gagal) set `PlayerPrefs "twoup.referrer_consumed"=1`.
5. Test `Assets/Tests/EditMode/RoomCodeSanitizerTests.cs` (`Sanitize_UppercasesAndStripsAmbiguousChars`: `"ab0o1i-cd"` → `"ABCD"`; `Sanitize_TruncatesToSix`), `DeepLinkParserTests.cs` (`Parse_ExtractsCodeFromHttpsAndCustomScheme`: dua URL → `"ABC234"`; `Parse_ReturnsNullForOtherPaths`), `InstallReferrerParserTests.cs` (`Parse_ExtractsRoomCodeFromUtmContent`; `Parse_ReturnsNullWithoutUtmContent`: `"utm_source=google-play&utm_medium=organic"` → null).
HUKUM SCRIPT: satu class per file, nama file = nama class.
**Acceptance criteria:**
- 4 file script + 3 file test ada di path persis; ketiga test class (6 method) hijau via batchmode EditMode; suite penuh pass.
- `Assets/Scripts/Logic/*.cs` tidak memuat `using UnityEngine` KECUALI tidak ada sama sekali (parser murni); `InstallReferrerReader.cs` satu-satunya yang menyentuh `AndroidJavaObject` dan hanya dalam `#if UNITY_ANDROID && !UNITY_EDITOR`.

## Task: Helper murni B — formatter & sorter (voting, async list, reflex, ledger)
**Priority:** 2
**Parallel safe:** true
**Description:**
LOGIKA/batch. Depends on: "Extend proto kontrak per TDD §3.1 + regen C#" (tipe `Twoup.V1.AsyncMatchSummary` dipakai). Namespace `TwoUp.Logic`, folder `Assets/Scripts/Logic/`, semua static class murni:
1. `VotingCountdownFormatter.cs`: `public static string Format(int remainingMs)` — CEILING ke detik ("3" untuk 2999ms, "1" untuk 1ms, "0" untuk ≤0). Keputusan plan: ceiling (TDD §7b menulis nama test keliru; expected value 2999→"3" yang berlaku).
2. `AsyncMatchListSorter.cs`: `public static List<Twoup.V1.AsyncMatchSummary> Sort(IEnumerable<Twoup.V1.AsyncMatchSummary> items)` — `your_turn==true` dulu, lalu `forfeit_deadline_unix_ms` menaik; deadline 0 (placement, tanpa deadline) di URUTAN TERAKHIR dalam kelompoknya. Stable.
3. `ReflexDuelStateFormatter.cs`: `public static string FormatReactionMs(int ms)` — `ms > 0` → `"{ms} ms"`; `ms <= 0` → `"—"`.
4. `LedgerDeltaFormatter.cs`:
```csharp
public static string FormatHeadline(int winsMine, int winsTheirs, string opponentName); // "You 7 : 5 Rina"
public static string FormatStreak(bool holderIsMe, string opponentName, int count);     // count < 2 -> ""; holderIsMe -> "You're on a {count} win streak!"; else "{opponentName} on a {count} win streak"
public static string FormatDuo(int score, int best, bool newBest);                      // newBest -> "New duo best! {best} → {score}"; else "Score {score} (best {best})"
```
   (format string PERSIS seperti di atas; GDD 5.1/5.2 copy).
5. Test: `VotingCountdownFormatterTests.cs` (`Format_CeilsToWholeSeconds`: 2999→"3", 3000→"3", 3001→"4", 0→"0", -5→"0"), `AsyncMatchListSorterTests.cs` (`Sort_YourTurnFirst_ThenByDeadlineAscending`, `Sort_ZeroDeadlineLast`), `ReflexDuelStateFormatterTests.cs` (`FormatReactionMs_HandlesZeroAndNegative`), `LedgerDeltaFormatterTests.cs` (`FormatHeadline_MatchesSpec`, `FormatStreak_EmptyBelowTwo`, `FormatDuo_NewBestArrowFormat` — assert string literal persis).
HUKUM SCRIPT: satu class per file.
**Acceptance criteria:**
- 4 script + 4 test file ada; 4 test class (8+ method) hijau via batchmode EditMode; suite penuh pass.
- `LedgerDeltaFormatter.FormatDuo(52, 52, true)` menghasilkan string yang memuat "New duo best!" (assertion di test).

## Task: SnapshotInterpolator untuk game real-time
**Priority:** 2
**Parallel safe:** true
**Description:**
LOGIKA/batch. Depends on: "Struktur asmdef + test scaffolding EditMode". Buat `Assets/Scripts/Logic/SnapshotInterpolator.cs`, namespace `TwoUp.Logic`, plain class (boleh `UnityEngine.Vector2`):
```csharp
public class SnapshotInterpolator
{
    public SnapshotInterpolator(float tickIntervalSeconds, float staleSnapSeconds); // dipakai 0.05f, 0.1f (TDD §4.7: tick 20Hz, snap >100ms)
    public void Push(Vector2 position, float receivedAtSeconds);   // snapshot server baru
    public Vector2 Sample(float nowSeconds);
}
```
Perilaku (TDD §4.7): simpan 2 snapshot terakhir (prev, latest). `Sample(now)`: lerp prev→latest dengan `t = (now - latest.receivedAt) / tickInterval` clamp 0..1 dimulai dari posisi prev saat t=0 — yaitu render tertinggal satu tick di belakang server (standar interpolasi snapshot). Kalau `now - latest.receivedAt > staleSnapSeconds` → return posisi latest apa adanya (berhenti gerak, tanpa ekstrapolasi). Sebelum ada snapshot → `Vector2.zero`; satu snapshot → posisi itu.
Test `Assets/Tests/EditMode/SnapshotInterpolatorTests.cs`: `Sample_MidTick_ReturnsMidpoint` (prev (0,0)@t=0, latest (100,0)@t=0.05, Sample(0.075) → x≈50 toleransi 0.01), `Sample_BeyondStale_SnapsToLatest` (Sample(0.2) → (100,0)), `Sample_NoData_ReturnsZero`, `Sample_SingleSnapshot_ReturnsIt`.
HUKUM SCRIPT: satu class per file.
**Acceptance criteria:**
- `SnapshotInterpolatorTests` (4 method) hijau via batchmode EditMode; suite penuh pass.
- `SnapshotInterpolator.cs` tidak memakai `Time.time`/`Time.deltaTime` (waktu selalu parameter).

## Task: Battleship placement — validator + generator deterministik
**Priority:** 2
**Parallel safe:** true
**Description:**
LOGIKA/batch. Depends on: "Extend proto kontrak per TDD §3.1 + regen C#" (tipe `Twoup.V1.ShipPlacement`). Folder `Assets/Scripts/Logic/`, namespace `TwoUp.Logic`:
1. `BattleshipPlacementValidator.cs`: `public static bool IsValid(IReadOnlyList<Twoup.V1.ShipPlacement> ships)` — tepat 5 kapal dengan multiset panjang {5,4,3,3,2}; setiap kapal in-bounds grid 10x10 (row/col 0-9, horizontal → col+length-1 ≤ 9, vertikal → row+length-1 ≤ 9); tidak ada dua kapal menempati sel sama. Juga `public static bool CellsOccupied(IReadOnlyList<ShipPlacement> ships, int row, int col)` untuk hint UI.
2. `BattleshipPlacementGenerator.cs`: `public static List<Twoup.V1.ShipPlacement> Generate(int seed)` — `System.Random(seed)`, tempatkan kapal urut panjang menurun [5,4,3,3,2], tiap kapal coba posisi+orientasi random sampai valid (retry bounded 1000, secara praktis selalu ketemu), hasil SELALU lolos `IsValid`. Deterministik: seed sama → hasil sama.
Test `Assets/Tests/EditMode/BattleshipPlacementTests.cs`: `Validator_AcceptsKnownGoodLayout` (layout hardcode), `Validator_RejectsOverlap`, `Validator_RejectsOutOfBounds`, `Validator_RejectsWrongShipSet` (4 kapal / panjang salah), `Generator_ProducesValidLayout` (seed 1..20 loop → semua `IsValid`), `Generator_IsDeterministic` (seed 7 dua kali → sequence identik).
HUKUM SCRIPT: satu class per file. Tidak menyentuh file lain.
**Acceptance criteria:**
- `BattleshipPlacementTests` (6 method) hijau via batchmode EditMode; suite penuh pass.
- `BattleshipPlacementGenerator.cs` memakai `System.Random` dengan seed parameter (grep; tidak ada `UnityEngine.Random`).

## Task: Platform plumbing — DeepLinkRouter, PushTokenClient, provider stub ads/IAP
**Priority:** 2
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "Helper murni A — RoomCodeSanitizer, DeepLinkParser, InstallReferrerParser" dan "RateGate + extend NetworkClient" — cek `Assets/Scripts/Logic/DeepLinkParser.cs` dan event `PairFoundReceived` di NetworkClient ada; kalau belum, berhenti dan laporkan.
1. `Assets/Scripts/App/DeepLinkRouter.cs` — MonoBehaviour, namespace `TwoUp` (akan dipasang di App object oleh task scene Boot; task ini HANYA script): `Awake()` → parse `Application.absoluteURL` via `DeepLinkParser.ExtractRoomCode`, subscribe `Application.deepLinkActivated += OnDeepLink`, panggil `InstallReferrerReader.ReadOnce(code => MatchContext.PendingRoomCode = code)`; hasil parse non-null → `MatchContext.PendingRoomCode = code`. `OnDestroy` unsubscribe.
2. `Assets/Scripts/App/PushTokenClient.cs` — MonoBehaviour, namespace `TwoUp`: method `public void OnIdentified()` (dipanggil BootController setelah ServerHello). Dalam `#if TWOUP_FIREBASE`: init `Firebase.Messaging`, ambil token, kirim `NetworkClient.Instance.Send(new Envelope { RegisterPushToken = new RegisterPushToken { FcmToken = token } })`, subscribe token-refresh untuk kirim ulang. `#else`: `Debug.Log("[Push] TWOUP_FIREBASE off — token registration skipped")`. File harus compile TANPA define aktif.
3. `Assets/Scripts/Monetization/IRewardedAdProvider.cs` (interface: `bool IsReady { get; }` + `void Show(System.Action<bool> onCompleted)`), `StubRewardedAdProvider.cs` (plain class: IsReady true, `Show` → `onCompleted(true)` sinkron), `IPurchaseProvider.cs` (`void PurchasePremium(System.Action<string> onToken, System.Action<string> onFailed)`), `StubPurchaseProvider.cs` (`onToken("stub-purchase-token")` sinkron). Semua namespace `TwoUp.Monetization`. Provider nyata (AdMob/IAP) BUKAN scope task ini — menyusul setelah SDK manual (define `TWOUP_ADS`/`TWOUP_IAP`).
4. Test `Assets/Tests/EditMode/MonetizationStubTests.cs`: `StubAd_CompletesTrue` (callback dipanggil dengan true), `StubPurchase_ReturnsToken`.
HUKUM SCRIPT: SETIAP class/interface di file sendiri (6 file), nama file = nama class/interface.
**Acceptance criteria:**
- 6 file script + 1 test file ada di path persis; `MonetizationStubTests` (2 method) hijau via batchmode EditMode; suite penuh pass TANPA define TWOUP_* aktif.
- `DeepLinkRouter.cs` dan `PushTokenClient.cs` masing-masing satu MonoBehaviour per file (HUKUM SCRIPT), tidak di-attach ke scene mana pun oleh task ini.

## Task: CHECKPOINT GATE LOGIKA — suite EditMode penuh hijau headless
**Priority:** 2
**Parallel safe:** false
**Description:**
LOGIKA/batch (gate). Depends on: SEMUA task Fase 0-1 di atas. Task verifikasi murni, TANPA perubahan kode kecuali perbaikan kecil yang ditemukan: jalankan suite EditMode penuh via batchmode dan pastikan SEMUA test dari fase 0-1 hijau (Smoke, ProtoContract, RateGate, GameCatalog, MatchContext, RoomCodeSanitizer, DeepLinkParser, InstallReferrerParser, VotingCountdownFormatter, AsyncMatchListSorter, ReflexDuelStateFormatter, LedgerDeltaFormatter, SnapshotInterpolator, BattleshipPlacement, MonetizationStub — minimal 15 test class). Audit tambahan: (a) `grep -r "TODO\|FIXME" Assets/Scripts/Logic/` = 0 hit; (b) tidak ada file dengan >1 class MonoBehaviour/ScriptableObject (cek manual file yang dibuat fase ini); (c) `Assets/Scripts/Logic/` tidak memuat MonoBehaviour sama sekali.
Kalau ada yang merah: perbaiki di task ini HANYA jika perbaikan ≤ 5 baris; lebih dari itu, laporkan gagal dengan daftar test merah (jangan tambal besar-besaran di gate).
**Acceptance criteria:**
- EditMode batchmode penuh exit 0 dengan ≥ 15 test class dan 0 fail (file hasil `.ccq-results-editmode.xml` memuat `failed="0"`).
- `Assets/Scripts/Logic/` bebas `MonoBehaviour` (grep `: MonoBehaviour` = 0 hit di folder itu) dan bebas TODO/FIXME.

---

## FASE 2 — Visual: scene authoring via builder script (priority 2, SEMUA serial `parallel_safe: false`, `live_editor: false` by design — lihat Global decisions #2)

Pola SETIAP task scene di fase ini (disalin ke tiap task, baca juga contoh nyata `Assets/Editor/SkeletonBuilder.cs` di repo):
- Builder static class di `Assets/Editor/SceneBuilders/<Nama>SceneBuilder.cs`, namespace `TwoUp.EditorTools`, method `public static void Build()` + `[MenuItem("2UP/Build Scenes/<Nama>")]`, memakai helper `UiKit` (task refactor). Scene disimpan ke `Assets/Scenes/<Nama>.unity`, didaftarkan via `UiKit.AddSceneToBuildSettings(path)` (idempoten).
- Worker menjalankan builder via batchmode `-executeMethod` di worktree-nya, lalu COMMIT `.unity` + `.meta` + script. Ini pengecualian sah (Global decisions #2).
- Controller MonoBehaviour di `Assets/Scripts/UI/<Nama>Controller.cs`; SEMUA referensi UI lewat `[SerializeField]` yang diisi builder via `UiKit.SetRef` — DILARANG `Find()` runtime. Services via `NetworkClient.Instance` / `AppStateMachine.Instance` (konvensi CLAUDE.md).
- Test hierarki di `Assets/Tests/EditMode/Scenes/<Nama>SceneTests.cs`: `EditorSceneManager.OpenScene("Assets/Scenes/<Nama>.unity", OpenSceneMode.Single)` lalu assert path objek + komponen + serialized ref non-null (pakai `SerializedObject` untuk baca field private).
- Acceptance standar (berlaku semua task scene, ditambah kriteria spesifik per task): (1) scene file + `.meta` ter-commit; (2) `grep "m_Script: {fileID: 0}"` pada scene yang disentuh = 0 hit; (3) test scene hijau via batchmode; (4) HUKUM SCRIPT satu class per file; (5) suite EditMode penuh pass; (6) semua teks pakai TMP, `raycastTarget=false` untuk teks/ikon non-interaktif.

## Task: Refactor builder — UiKit + per-scene builder + SceneAsserts
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "CHECKPOINT GATE LOGIKA". Refactor TANPA mengubah tampilan scene existing:
1. Buat `Assets/Editor/SceneBuilders/UiKit.cs` (static, namespace `TwoUp.EditorTools`): PINDAHKAN helper dari `Assets/Editor/SkeletonBuilder.cs` — `CreateCanvasWithScreen`, `CreateUIObject`, `CreatePanel`, `CreateText`, `CreateButton`, `CreateRoomCodeInput` (rename generik: `CreateInputField(Transform parent, string name, string placeholder, int charLimit)`), `Place`, `StretchFull`, `SetRef`, `SetArray`, `NewScene`, `SaveScene` — jadikan `public`. Tambah `public static void AddSceneToBuildSettings(string scenePath)` (append idempoten ke `EditorBuildSettings.scenes`, enabled true) dan `public static GameObject CreateListRowTemplate(Transform parent, string name)` (row inactive: panel + TMP label kiri + TMP label kanan; template list untuk di-clone runtime — WAJIB sibling container, bukan anak container yang dibersihkan).
2. Pecah `SkeletonBuilder.cs`: buat `BootSceneBuilder.cs`, `ConnectFourSceneBuilder.cs`, `LobbySceneBuilder.cs` di `Assets/Editor/SceneBuilders/` yang memuat method build existing (isi fungsi SAMA, hanya pindah + pakai UiKit); `SkeletonBuilder.cs` tersisa: `BuildAll()` (memanggil semua builder terdaftar), `ImportTmpEssentials*`, `CreateServerConfig`, `ConfigureBuildSettings` (hapus — diganti AddSceneToBuildSettings per builder), `ConfigurePlayerSettings`, `BuildApk`.
3. Jalankan `BuildAll` batchmode → regenerate `Boot.unity`/`Lobby.unity`/`ConnectFour.unity`, commit.
4. Test `Assets/Tests/EditMode/Scenes/SceneAsserts.cs` (static helper: `OpenScene(string path)`, `AssertObject(string hierarchyPath)`, `AssertRefNotNull(Component c, string fieldName)`) + `BaselineSceneTests.cs`: `Boot_HasAppAndBootUi` (App punya NetworkClient+AppStateMachine, Screen_Boot punya BootController dengan statusText/retryButton non-null), `ConnectFour_Has42CellsAnd7Columns` (cells arraySize 42 semua non-null via SerializedObject, columnButtons 7).
**Acceptance criteria:**
- Standar pola scene (lihat atas) + `Assets/Editor/SceneBuilders/UiKit.cs` ada dengan `AddSceneToBuildSettings` dan `CreateListRowTemplate` (grep).
- `BaselineSceneTests` (2 method) hijau via batchmode; 3 scene existing tetap ada di `EditorBuildSettings` (grep path di `ProjectSettings/EditorBuildSettings.asset`).
- `SkeletonBuilder.cs` tidak lagi memuat method `BuildBootScene/BuildLobbyScene/BuildConnectFourScene` (sudah pindah).

## Task: Shared HUD kit — EmoteWheelController + ConnectionIndicator + builder helper
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Refactor builder — UiKit + per-scene builder + SceneAsserts".
1. `Assets/Scripts/UI/EmoteWheelController.cs` (MonoBehaviour, namespace `TwoUp.UI`): `[SerializeField] Button toggleButton; [SerializeField] GameObject wheelPanel; [SerializeField] Button[] emoteButtons; [SerializeField] TMP_Text incomingEmoteText;` + `RateGate` instance (3f detik — GDD: cooldown emote 3s). Toggle buka/tutup panel; klik emote ke-i → kalau `RateGate.TryPass("emote", Time.unscaledTime)` → `NetworkClient.Instance.Send(new Envelope { EmoteSend = new EmoteSend { MatchId = MatchContext.MatchId, EmoteId = EmoteIds[i] } })` + tutup panel; subscribe `EmoteBroadcastReceived` → tampilkan `"{displayName pengirim dari MatchContext}: {label}"` di `incomingEmoteText` selama 2.5 detik (coroutine). `public static readonly string[] EmoteIds = { "emote_thumbsup","emote_lol","emote_wow","emote_cry","emote_fire","emote_gg" };` label tampil `+1, LOL, WOW, CRY, FIRE, GG` (placeholder teks — Global decisions #5).
2. `Assets/Scripts/UI/ConnectionIndicator.cs` (MonoBehaviour): `[SerializeField] TMP_Text label;` — subscribe `NetworkClient.Instance.Connected/Disconnected`, teks "●" hijau (koneksi ok, warna via `label.color`) / "○ reconnecting" merah.
3. `UiKit` tambah: `public static EmoteWheelController BuildEmoteWheel(Transform screenRoot)` (tombol 96x96 pojok kanan-bawah anchor (1,0); panel wheel inactive berisi 6 tombol teks grid 3x2; incoming text anchor bawah-tengah) dan `public static ConnectionIndicator BuildConnectionBadge(Transform screenRoot)` (pojok kiri-atas anchor (0,1), 120x48). Semua referensi di-wire via `SetRef`/`SetArray`.
4. Tidak mengubah scene apa pun di task ini (helper dipakai task scene berikutnya). Test: `Assets/Tests/EditMode/EmoteWheelTests.cs` — `EmoteIds_HasSixBaseEmotes` (urutan persis di atas).
**Acceptance criteria:**
- Standar pola scene + 2 script controller di file masing-masing; `UiKit.cs` memuat `BuildEmoteWheel` dan `BuildConnectionBadge` (grep).
- `EmoteWheelTests` hijau; suite EditMode penuh pass; tidak ada scene `.unity` yang berubah di diff task ini.

## Task: Scene Home (S2)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Shared HUD kit". Pola standar fase 2 (lihat header fase). Builder `HomeSceneBuilder.cs` → `Assets/Scenes/Home.unity`; controller `Assets/Scripts/UI/HomeController.cs`.
Hierarki wajib di bawah `UICanvas/Screen_Home`: `Title` (TMP "2UP", top center), `Btn_PlayWithFriend` (primary, 700x130, y+380 — TOMBOL PALING PROMINENT per GDD D2), `Btn_QuickMatch` (700x110, y+230), `Btn_VsBot` (700x110, y+100), `Badge_AsyncCount` (pill 220x64 kanan-atas Btn kanan; TMP text; inactive default), `Row_Nav` (bottom bar: `Btn_Profile`, `Btn_Shop`, `Btn_Settings` masing-masing 220x100), `Btn_AsyncMatches` (500x100, y-60, label "Async Matches").
HomeController: `[SerializeField]` semua button + badge + badge text. Start: wire onClick → `AppStateMachine.Instance.ToInvite()/ToQueue()/…ToVoting() dengan MatchContext.VsBotMode=true (Btn_VsBot)/ToProfile()/ToShop()/ToSettings()/ToAsyncList()`; `MatchContext.VsBotMode=false` untuk semua jalur non-bot; kirim `new Envelope { ListAsyncMatches = new ListAsyncMatches() }` via NetworkClient; subscribe `AsyncMatchListReceived` → hitung `matches.Count(m => m.YourTurn)` → >0: badge aktif + text angka, 0: badge inactive. OnDestroy unsubscribe. JANGAN mengubah Boot/Lobby di task ini.
Test `HomeSceneTests.cs`: `Home_HasAllButtonsWired` (semua path objek ada; semua serialized ref HomeController non-null), `Home_BadgeInactiveByDefault`.
**Acceptance criteria:**
- Standar pola scene fase 2 + `Assets/Scenes/Home.unity` ada, terdaftar di EditorBuildSettings (grep `Scenes/Home.unity` di `ProjectSettings/EditorBuildSettings.asset`).
- `HomeSceneTests` (2 method) hijau via batchmode; `HomeController.cs` tidak memuat `GameObject.Find` (grep = 0).

## Task: Extend scene Boot — App wiring baru + alur ke Home
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Home (S2)" — cek `Assets/Scenes/Home.unity` ada; kalau belum, berhenti dan laporkan.
1. Edit `BootSceneBuilder.cs`: App object sekarang juga dapat komponen `DeepLinkRouter` + `PushTokenClient` (script sudah ada dari fase 1), dan `AppStateMachine.catalog` di-wire ke `Assets/Config/GameCatalog.asset` via `UiKit.SetRef`.
2. Edit `Assets/Scripts/UI/BootController.cs`: `OnServerHello` — panggil `GetComponent`-free path: `FindFirstObjectByType<PushTokenClient>()` DILARANG; ganti: BootController dapat `[SerializeField] PushTokenClient pushTokenClient;` (di-wire builder). Urutan di OnServerHello: `pushTokenClient.OnIdentified()`; lalu kalau `MatchContext.PendingRoomCode` non-null/non-empty → `AppStateMachine.Instance.ToInvite()` — JANGAN null-kan field-nya di sini; InviteRoomController yang mengonsumsinya saat Start. Tanpa kode → `AppStateMachine.Instance.ToHome()` (ganti dari `ToLobby()`).
3. Jalankan builder Boot batchmode, commit scene.
Test extend `BaselineSceneTests.Boot_HasAppAndBootUi` → tambah assert App punya `DeepLinkRouter`+`PushTokenClient`, `AppStateMachine.catalog` non-null, `BootController.pushTokenClient` non-null.
**Acceptance criteria:**
- Standar pola scene fase 2; `Boot.unity` ter-regenerate; `BootController.cs` memuat `ToHome()` dan TIDAK memuat `ToLobby()` (grep).
- `BaselineSceneTests` hijau (assert baru); suite penuh pass.

## Task: Retire Lobby — hapus scene, controller, builder
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Extend scene Boot — App wiring baru + alur ke Home" — cek `BootController.cs` sudah memanggil `ToHome()` (grep); kalau belum, berhenti dan laporkan. Penghapusan DISENGAJA (bagian dari plan, TDD §5 S1/S2: Lobby digantikan Home/InviteRoom/Queue):
1. Hapus `Assets/Scenes/Lobby.unity` + `.meta`, `Assets/Scripts/UI/LobbyController.cs` + `.meta`, `Assets/Editor/SceneBuilders/LobbySceneBuilder.cs` + `.meta`.
2. Hapus entri Lobby dari `EditorBuildSettings` (edit `UiKit`/builder yang mendaftarkannya; regenerate build settings tanpa Lobby).
3. Hapus `ToLobby()` + konstanta "Lobby" dari `AppStateMachine.cs`; hapus `State.Lobby` dari enum HANYA kalau tidak dipakai referensi lain (grep dulu; kalau dipakai, laporkan file mana).
4. Grep seluruh `Assets/` untuk "Lobby" — sisa referensi hanya boleh di komentar sejarah/docs; kode aktif = 0.
**Acceptance criteria:**
- `Assets/Scenes/Lobby.unity`, `LobbyController.cs`, `LobbySceneBuilder.cs` tidak ada; pasangan `.meta`-nya juga (tidak ada meta yatim).
- `ProjectSettings/EditorBuildSettings.asset` tidak memuat `Lobby.unity` (grep = 0); `AppStateMachine.cs` tidak memuat `ToLobby` (grep = 0).
- Suite EditMode penuh pass (compile bersih membuktikan tidak ada referensi menggantung).

## Task: Scene InviteRoom (S3)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Retire Lobby". Pola standar fase 2. Builder `InviteRoomSceneBuilder.cs` → `Assets/Scenes/Invite.unity` (nama scene "Invite" sesuai `AppStateMachine.ToInvite`); controller `InviteRoomController.cs`.
Sebelum builder: edit `Assets/Scripts/Net/ServerConfig.cs` tambah field `public string inviteLinkBase = "https://2up.example/r/";` (placeholder — Blocker B2 TDD; JANGAN hardcode di controller).
Hierarki `UICanvas/Screen_Invite`: `Btn_Back` (kiri-atas), `Panel_CreateRoom` (atas): `Text_RoomCode` (TMP 90pt), `Text_TtlCountdown` (TMP 36pt), `Btn_CopyLink` (label "Copy invite link"), `Text_WaitingOpponent`; `Panel_JoinRoom` (bawah): `Input_RoomCode` (via `UiKit.CreateInputField`, charLimit 6), `Btn_Join`; `Text_Toast` (inactive).
InviteRoomController — Start: (a) kalau `MatchContext.PendingRoomCode` terisi → isi `Input_RoomCode.text`, kirim `JoinRoom{room_code}` langsung, `MatchContext.PendingRoomCode=null`; (b) selain itu kirim `CreateRoom{}`. Subscribe: `RoomCreatedReceived` → tampil kode + simpan `expires_at_unix_ms` → update `Text_TtlCountdown` tiap Update format `mm:ss` sisa waktu (pakai `System.DateTimeOffset.FromUnixTimeMilliseconds`); `PairFoundReceived` → persis: `MatchContext.PairId = msg.PairId; AppStateMachine.Instance.ToVoting();`; `RoomJoinPendingReceived` → toast "{invitee} joined — starting…"; `RoomExpiredReceived` → toast "Room expired. Create a new one!" + kirim `CreateRoom{}` baru; `ErrorReceived` → toast message. `Btn_CopyLink`: controller TIDAK memegang ServerConfig langsung — tambahkan di `NetworkClient.cs` property `public string InviteLinkBase => serverConfig.inviteLinkBase;`, lalu `GUIUtility.systemCopyBuffer = NetworkClient.Instance.InviteLinkBase + kodeRoom;` + toast "Link copied". `Btn_Join` → sanitize via `RoomCodeSanitizer.Sanitize(Input.text)` → hasil kosong → toast "Enter a room code"; else kirim `JoinRoom`. `Btn_Back` → langsung `ToHome()` tanpa pesan ke server (tidak ada close-room di kontrak; `TODO(contract)` sudah tercatat di TDD).
Test `InviteSceneTests.cs`: `Invite_HasCreateAndJoinPanels` (path + ref non-null semua field controller), `Invite_InputCharLimitSix` (TMP_InputField.characterLimit == 6).
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Invite.unity` terdaftar di build settings; `ServerConfig.cs` memuat `inviteLinkBase` dan `NetworkClient.cs` memuat `InviteLinkBase` (grep).
- `InviteSceneTests` (2 method) hijau; `InviteRoomController.cs` memuat `RoomCodeSanitizer.Sanitize` (grep — sanitasi wajib jalan).

## Task: Scene Queue (S4)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Retire Lobby". Pola standar fase 2. Builder `QueueSceneBuilder.cs` → `Assets/Scenes/Queue.unity`; controller `QueueController.cs`.
Hierarki `UICanvas/Screen_Queue`: `Text_Status` (TMP 52pt, "Finding an opponent…"), `Text_Hint` (TMP 34pt, "~10s — a bot joins if nobody's around", per GDD D4 transparansi bot), `Btn_Cancel` (500x110).
QueueController — Start: kirim `JoinQueue{}`; subscribe `PairFoundReceived` → `MatchContext.PairId = msg.PairId;` + `Text_Status.text="Match found!"` + `AppStateMachine.Instance.ToVoting()` (langsung; animasi matchfound = polish nanti); `ErrorReceived` → status = error message + tampilkan cancel. `Btn_Cancel` → kirim `LeaveQueue{}` + `ToHome()`. OnDestroy unsubscribe.
Test `QueueSceneTests.cs`: `Queue_HasStatusAndCancelWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Queue.unity` terdaftar; `QueueSceneTests` hijau.
- `QueueController.cs` mengirim `JoinQueue` di `Start` dan `LeaveQueue` di handler cancel (grep `JoinQueue` dan `LeaveQueue`).

## Task: Scene Voting (S5) + bot picker
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Retire Lobby". Pola standar fase 2. Builder `VotingSceneBuilder.cs` → `Assets/Scenes/Voting.unity`; controller `VotingController.cs` + component kecil `GameCardView.cs` (MonoBehaviour: `[SerializeField] TMP_Text nameLabel; [SerializeField] TMP_Text tagsLabel; [SerializeField] Image background; [SerializeField] Button button; public string GameId;` — file sendiri!).
Builder: baca `GameCatalog.asset` saat BUILD, buat 6 kartu `Card_<gameId>` (grid 2 kolom x 3 baris, kartu 480x280) di `Grid_GameCards`, tiap kartu = GameCardView dengan nameLabel=displayName, tagsLabel="Versus|Co-op · Live|Turn-based" sesuai entry, background warna flat berbeda per kartu (palet bebas builder, konsisten), `GameId` di-set serialized. Elemen lain `UICanvas/Screen_Voting`: `Text_Header` ("Pick your game!"), `Text_Subheader` ("Both agree = instant start"), `Text_PairBadge` (dari PairFound milestone/duo — format `"{milestone_tier} · Duo Lv{duo_level}"`, sembunyikan kalau total_matches 0), `Text_Countdown` (kanan-atas), `Panel_Showdown` (inactive: `Text_ShowdownCopy` "Pick one to agree, or we'll flip a coin!", dua slot `Slot_Mine`/`Slot_Theirs` yang diisi runtime dengan REPOSISI kartu existing — TIDAK instantiate baru: pindahkan transform kartu terpilih ke slot), `Panel_BotPicker` (inactive: reuse info — 3 tombol `Btn_TierEasy/Medium/Hard` toggle-style + `Btn_StartBot`), `Text_Toast` (inactive).
VotingController — Start: kalau `MatchContext.VsBotMode` → aktifkan `Panel_BotPicker`, sembunyikan countdown/subheader; pilih kartu = set `selectedGameId`, pilih tier = simpan `"easy"/"medium"/"hard"` (default "medium"), `Btn_StartBot` → `StartBotMatch{game_id=selectedGameId, tier=tier}`; tanpa kartu terpilih → toast "Pick a game first". Mode normal: kartu klik → `VoteGame{pair_id=MatchContext.PairId, game_id}`; subscribe `VoteUpdateReceived` → highlight kartu pilihan sendiri (border putih = scale 1.05) dan pilihan lawan (ikon "them" = tagsLabel prefix "◈ "); `VotingShowdownReceived` → aktifkan Panel_Showdown, pindahkan 2 kartu kandidat ke slot; klik kartu LAWAN saat showdown → `ShowdownPick{pair_id, game_id}`; `VotingLockedReceived` → countdown lokal dari `countdown_ms` via `VotingCountdownFormatter.Format`; `MatchFoundReceived` (existing event) → `MatchContext.Set(msg, NetworkClient.Instance.PlayerId)`; `GameStartReceived` → `MatchContext.PendingGameStart=msg; MatchContext.MatchId=msg.MatchId; AppStateMachine.Instance.ToGame(MatchContext.GameId)`; `VotingCancelledReceived` → toast "They left 😢" + tampilkan `Btn_FindNew` (→ToQueue) + `Btn_Home` (→ToHome) (dua tombol ini inactive default, bagian hierarki builder). Countdown display 15s lokal sejak Start (server autoritatif; display only).
Test `VotingSceneTests.cs`: `Voting_HasSixCardsMatchingCatalog` (6 GameCardView, GameId set = 6 id kanonik), `Voting_ShowdownAndBotPickerInactiveByDefault`, `Voting_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Voting.unity` terdaftar; `VotingSceneTests` (3 method) hijau.
- `GameCardView.cs` file sendiri (HUKUM SCRIPT); `VotingController.cs` mengirim `VoteGame`, `ShowdownPick`, `StartBotMatch` (grep ketiganya).

## Task: Scene Result (S7)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Voting (S5) + bot picker". Pola standar fase 2. Builder `ResultSceneBuilder.cs` → `Assets/Scenes/Result.unity`; controller `ResultController.cs`.
Hierarki `UICanvas/Screen_Result`: `Text_Headline` (72pt: "You win!/You lose/Draw!"), `Text_LedgerLine` (44pt), `Text_StreakLine` (36pt), `Text_SeriesLine` (36pt, "Series 2 : 1"), `Btn_Rematch` (560x120), `Btn_NextGame` (560x110), `Btn_Leave` (560x100), `Text_OpponentDecision` (34pt), `Text_Countdown` (kanan-atas, 20s lokal).
ResultController — Start (sumber data `MatchContext.LastGameOver` yang di-set controller game sebelum `ToResult()`; null → langsung `ToHome()` + log error): headline dari `LastGameOver.Result` (draw / WinnerPlayerIds berisi `NetworkClient.Instance.PlayerId`); co-op (`MatchContext` game mode dari `AppStateMachine.Instance.Catalog.Find(MatchContext.GameId).mode == CoOp`) → `Text_LedgerLine = LedgerDeltaFormatter.FormatDuo(result.CoOpScore, …, result.CoOpNewBest)` (best = CoOpScore kalau NewBest, else tampilkan skor saja — pakai overload FormatDuo(score, best:score, newBest)); versus vs manusia (`MatchContext.Opponent != null && !MatchContext.Opponent.IsBot`) → kirim `GetPairDetail{other_player_id=opponent}` dan saat `PairDetailReceived` → `FormatHeadline(wins_mine, wins_theirs, opponentName)` + `FormatStreak(...)`; vs bot → `Text_LedgerLine = "vs {name} (AI)"`. Series: update `MatchContext.SeriesWinsMine/Theirs` dari hasil SEBELUM render ("Series {mine} : {theirs}"; sembunyikan kalau 0:0). Buttons: `Btn_Rematch` → `RematchRequest{match_id, accept:true, choice:RematchChoice.RematchSameGame}` + disable tombol + `Text_OpponentDecision="Waiting…"`; `Btn_NextGame` → `{accept:true, choice:NextGame}`; `Btn_Leave` → `{accept:false}` + `MatchContext.Clear()` + `ToHome()`. Subscribe: `RematchStatusUpdateReceived` → kalau player_id != milikku → teks `"{opponent} wants a rematch!"` / `"…wants a different game"` sesuai choice; `GameStartReceived` → `PendingGameStart` + `ToGame(MatchContext.GameId)` (rematch same game); `PairFoundReceived` → `PairId` + `ToVoting()` (jalur next game); `ErrorReceived` code `rematch_declined`/`rematch_timeout` → `MatchContext.Clear()` kecuali series? — KEPUTUSAN: clear semua + toast "{opponent} left. GG!" 1.5s lalu `ToHome()`.
Test `ResultSceneTests.cs`: `Result_HasAllControlsWired`, `Result_CountdownTextExists`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Result.unity` terdaftar; `ResultSceneTests` (2 method) hijau.
- `ResultController.cs` memuat `RematchChoice.NextGame` dan `GetPairDetail` (grep — dua jalur kontrak dipakai).

## Task: Scene AsyncMatches (S11)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Result (S7)". Pola standar fase 2. Builder `AsyncMatchesSceneBuilder.cs` → `Assets/Scenes/AsyncList.unity`; controller `AsyncMatchesController.cs` + `AsyncMatchRowView.cs` (MonoBehaviour, file sendiri: `[SerializeField] TMP_Text titleLabel; [SerializeField] TMP_Text statusLabel; [SerializeField] Button button; public string MatchId;`).
Hierarki `UICanvas/Screen_AsyncList`: `Btn_Back`, `Text_Title` ("Async Matches"), ScrollView standar UGUI (`ScrollRect` + Viewport/RectMask2D + `Content` VerticalLayoutGroup+ContentSizeFitter pivot (0.5,1)) dengan `RowTemplate` = `AsyncMatchRowView` INACTIVE sebagai SIBLING Content (BUKAN anak Content — hukum populate-from-template), `Text_Empty` ("No async matches").
AsyncMatchesController — Start: kirim `ListAsyncMatches{}`; `AsyncMatchListReceived` → sort via `AsyncMatchListSorter.Sort`, clear anak Content (template aman karena sibling), clone template per row: title `"{game displayName} vs {opponent_display_name}"`, status `your_turn ? "YOUR TURN" : "waiting…"` + sisa deadline (`forfeit_deadline_unix_ms`>0 → "{h}h left"), `SetActive(true)` WAJIB setelah Instantiate, onClick → kirim `ResumeAsyncMatch{match_id}`; list kosong → `Text_Empty` aktif. `MatchResumedReceived` → `MatchContext.MatchId=msg.MatchId; MatchContext.PendingResumeState=msg.State; AppStateMachine.Instance.ToGame(msg.GameId)`. `Btn_Back` → ToHome.
Test `AsyncListSceneTests.cs`: `AsyncList_TemplateIsInactiveSiblingOfContent` (template inactive DAN parent-nya ≠ Content), `AsyncList_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/AsyncList.unity` terdaftar; `AsyncListSceneTests` (2 method) hijau.
- `AsyncMatchesController.cs` memuat `SetActive(true)` setelah Instantiate (grep) dan memakai `AsyncMatchListSorter.Sort` (grep).

## Task: Scene Profile (S8)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene AsyncMatches (S11)". Pola standar fase 2. Builder `ProfileSceneBuilder.cs` → `Assets/Scenes/Profile.unity`; controller `ProfileController.cs` + `PairRowView.cs` (file sendiri: `[SerializeField] TMP_Text nameLabel; [SerializeField] TMP_Text scoreLabel; [SerializeField] TMP_Text badgeLabel; [SerializeField] Button button; public string OtherPlayerId;`).
Hierarki `UICanvas/Screen_Profile`: `Btn_Back`, `Panel_Me` (`Text_MyName` + `Input_EditName` (charLimit 20, inactive default) + `Btn_EditName` + `Text_BotStats` "vs Bots: {w}W {l}L {d}D"), ScrollView `List_Pairs` (pola template sibling sama seperti AsyncList) dengan `PairRowTemplate`, `Panel_PairDetail` (inactive: `Text_DetailHeader`, `Text_DetailAggregate`, `Text_DetailStreak`, `Text_VersusLines` (multiline TMP), `Text_CoopLines` (multiline), `Btn_CloseDetail`).
ProfileController — Start: kirim `GetProfile{}`; `ProfileDataReceived` → nama, bot stats, populate rows (per `PairSummary`: nameLabel=other_display_name, scoreLabel=`LedgerDeltaFormatter.FormatHeadline(wins_mine, wins_theirs, other_display_name)`, badgeLabel=`milestone_tier=="none" ? "Duo Lv{duo_level}" : "{milestone_tier} · Duo Lv{duo_level}"`); row click → `GetPairDetail{other_player_id}`; `PairDetailReceived` → aktifkan Panel_PairDetail, isi: aggregate via FormatHeadline, streak via FormatStreak, versus_lines per baris `"{game_id}: {wins_mine} : {wins_theirs} ({draws} draws)"`, coop_lines `"{game_id}: best {best_score} ({total_matches} runs)"`. `Btn_EditName` → toggle input; submit (onEndEdit) → non-empty → `SetProfile{display_name=trimmed}` → `ProfileDataReceived` refresh. `Btn_Back` → ToHome. (Tab Versus/Co-op dari GDD disederhanakan jadi dua blok teks di detail — keputusan plan, polish nanti.)
Test `ProfileSceneTests.cs`: `Profile_TemplateSiblingPatternCorrect`, `Profile_DetailPanelInactiveByDefault`, `Profile_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Profile.unity` terdaftar; `ProfileSceneTests` (3 method) hijau.
- `ProfileController.cs` mengirim `GetProfile`, `GetPairDetail`, `SetProfile` (grep ketiganya).

## Task: Scene Shop (S9)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Profile (S8)". Pola standar fase 2. Builder `ShopSceneBuilder.cs` → `Assets/Scenes/Shop.unity`; controller `ShopController.cs` + `ShopItemRowView.cs` (file sendiri: `[SerializeField] TMP_Text nameLabel; [SerializeField] TMP_Text priceLabel; [SerializeField] Button buyButton; public string ItemKey;`).
Hierarki `UICanvas/Screen_Shop`: `Btn_Back`, `Text_TicketBalance` ("🎟 {n}" — pakai teks "Tickets: {n}", tanpa emoji, font default), `Btn_WatchAd` (label "Watch ad (+1 ticket) — {sisa}/5 today"), ScrollView `List_Items` pola template sibling (`ShopItemRowTemplate`), `Btn_BuyPremium` (label "2UP Premium $2.99"), `Text_Toast` (inactive).
ShopController — field `IRewardedAdProvider adProvider = new StubRewardedAdProvider(); IPurchaseProvider purchaseProvider = new StubPurchaseProvider();` (instansiasi langsung; saat SDK nyata masuk, hanya dua baris ini berubah di belakang define — catat komentar). Start: kirim `GetShop{}`; `ShopDataReceived` → balance, ads_remaining ke label; populate rows: nameLabel=item_key, priceLabel = owned ? "OWNED" : (premium_exclusive ? "PREMIUM" : "{price} tickets"), buyButton interactable = !owned && !premium_exclusive; buy click → `PurchaseItem{item_key}`; `WalletUpdateReceived` → update balance/ads label + kalau `unlocked_item_key` non-empty → toast "Unlocked {key}!" + refresh `GetShop{}`; `ErrorReceived` → toast (code `insufficient_tickets` → "Not enough tickets — watch an ad!"). `Btn_WatchAd` → `adProvider.Show(ok => { if (ok) NetworkClient…Send(ClaimAdTicket) })`. `Btn_BuyPremium` → `purchaseProvider.PurchasePremium(token => Send(PremiumPurchased{play_purchase_token=token}), err => toast(err))`; `ProfileDataReceived` (balasan premium) → toast "Premium active!". `Btn_Back` → ToHome.
Test `ShopSceneTests.cs`: `Shop_TemplateSiblingPatternCorrect`, `Shop_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Shop.unity` terdaftar; `ShopSceneTests` (2 method) hijau.
- `ShopController.cs` memuat `StubRewardedAdProvider` dan mengirim `ClaimAdTicket`, `PurchaseItem`, `PremiumPurchased` (grep).

## Task: Scene Settings (S10)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Shop (S9)". Pola standar fase 2. Builder `SettingsSceneBuilder.cs` → `Assets/Scenes/Settings.unity`; controller `SettingsController.cs`.
Hierarki `UICanvas/Screen_Settings`: `Btn_Back`, tiga baris toggle (`Toggle_Sound`, `Toggle_Music`, `Toggle_Vibration` — UGUI Toggle via builder: background+checkmark pakai sprite builtin `UISprite`/`Checkmark`), `Btn_RestorePurchase`, `Text_Links` (TMP dengan "Privacy Policy · Terms" — link handler TIDAK perlu, plain text + TODO(design) URL), `Text_Version`.
SettingsController — Start: load `PlayerPrefs` keys `twoup.sound`/`twoup.music`/`twoup.vibration` (default 1) ke toggle; `onValueChanged` → simpan PlayerPrefs + `AudioListener.volume = sound ? 1f : 0f` (musik/vibration disimpan saja — belum ada audio asset/haptic, konsumen menyusul); `Text_Version.text = "v" + Application.version`; `Btn_RestorePurchase` → kirim `GetProfile{}` dan pada `ProfileDataReceived` kalau `premium` → toast "Premium restored", else toast "No purchase found" (server adalah source of truth premium — MVP restore = re-query); `Btn_Back` → ToHome.
Test `SettingsSceneTests.cs`: `Settings_HasTogglesAndVersion` (3 Toggle + Text_Version ada, ref wired).
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Settings.unity` terdaftar; `SettingsSceneTests` hijau.
- `SettingsController.cs` memakai `PlayerPrefs` keys persis `twoup.sound`, `twoup.music`, `twoup.vibration` (grep).

## Task: Extend scene ConnectFour — turn timer ring, emote wheel, jalur async
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene Settings (S10)". Pola standar fase 2 — scene existing `Assets/Scenes/ConnectFour.unity` di-regenerate dari `ConnectFourSceneBuilder.cs` yang di-edit. JANGAN mengubah logika grid/kolom/render existing di `ConnectFourController.cs` — hanya TAMBAH.
1. Builder: tambah `Ring_TurnTimer` (Image `type=Filled`, `fillMethod=Radial360`, sprite builtin Knob, 110x110, sebelah `TurnText`), panggil `UiKit.BuildEmoteWheel(screen)` + `UiKit.BuildConnectionBadge(screen)`, `Text_Toast` (inactive). Wire field baru controller: `[SerializeField] Image turnTimerRing; [SerializeField] TMP_Text toastText;`.
2. `ConnectFourController.cs` tambah: (a) di `Render(state)` — restart countdown lokal 30s tiap kali `next_player_id` BERUBAH (simpan `lastNextPlayerId`); `Update()` → `turnTimerRing.fillAmount = sisa/30f`; ring hanya aktif kalau `MatchContext.PendingResumeState == null && !gameOver` (match live; mode async tanpa timer — AC-C4-07); TIDAK ada aksi lokal saat 0 (server yang men-skip — AC-C4-06 server-authoritative; ring display only). (b) Start: kalau `MatchContext.PendingResumeState != null` → `RenderStateBytes(MatchContext.PendingResumeState)` + `MatchContext.PendingResumeState=null` (jalur resume async, setara PendingGameStart). (c) subscribe `MatchWentAsyncReceived` → toast "Match continues in Async Matches" 2 detik → `MatchContext.Clear(); AppStateMachine.Instance.ToHome();`. (d) `OnGameOver` existing: TAMBAH `MatchContext.LastGameOver = msg;` lalu ganti panel game-over internal menjadi: simpan lalu `AppStateMachine.Instance.ToResult()` — HAPUS panel gameOver lama + rematch internal dari builder & controller (Result scene yang memegang flow itu sekarang; hapus field gameOverPanel/resultText/rematchStatusText/rematchButton/backButton + handler `OnRematch`/`OnRematchRequest`-nya; `OnGameStart` rematch-restart pindah ke ResultController).
3. Regenerate scene, commit.
Test extend `BaselineSceneTests.ConnectFour_Has42CellsAnd7Columns` tetap; tambah `ConnectFour_HasTimerRingAndEmoteWheel` (Ring_TurnTimer Image filled + EmoteWheelController ada, refs wired), `ConnectFour_NoLegacyGameOverPanel` (objek `Panel_GameOver` TIDAK ada di scene).
**Acceptance criteria:**
- Standar pola scene fase 2; `ConnectFour.unity` ter-regenerate; kedua test baru + baseline hijau.
- `ConnectFourController.cs` memuat `MatchContext.LastGameOver` dan `ToResult()` (grep), TIDAK lagi memuat `rematchButton` (grep = 0).

## Task: Scene ReflexDuel (S6-RD)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Extend scene ConnectFour". Pola standar fase 2. Builder `ReflexDuelSceneBuilder.cs` → `Assets/Scenes/ReflexDuel.unity`; controller `ReflexDuelController.cs`.
Hierarki `UICanvas/Screen_ReflexDuel`: `Panel_Wait` (fullscreen gelap, `Text_Wait` "Wait for it…"), `Panel_Go` (fullscreen hijau terang, inactive, `Text_Go` "GO!"), `Btn_TapZone` (fullscreen transparan raycastable Button, transition None — SELALU aktif saat playing), `Row_RoundPips` (5 Image kecil: warna abu=belum, hijau=menang, merah=kalah), `Text_Score` ("2 — 1"), `Panel_RoundResult` (inactive: `Text_MyMs`, `Text_TheirMs`, `Text_RoundVerdict` "Too soon!"/"Round won!"), emote wheel + connection badge via UiKit, `Text_Toast`.
ReflexDuelController — Start: `NetworkClient.Instance.PingIntervalSeconds = 2f;` (kompensasi RTT TDD §4.7), render dari `PendingGameStart`/`PendingResumeState` pola sama ConnectFour; `OnDestroy`: kembalikan `15f`. `Btn_TapZone.onClick` → `Send(GameInput{ match_id, input = new ReflexDuelInput().ToByteString() })` (tap SELALU dikirim; server yang menilai false start — AC-RD-04). `OnGameState` → parse `ReflexDuelState`: `phase==RD_WAITING` → Panel_Wait aktif, Panel_Go mati; `RD_ARMED` → Panel_Go aktif; `RD_RESOLVED` → Panel_RoundResult: `Text_MyMs = "You: " + ReflexDuelStateFormatter.FormatReactionMs(msSeatSaya)` (pilih field seat via `MatchContext.MySeat`), `Text_TheirMs` idem lawan, verdict dari `last_round_false_start_seat{ku}` → "Too soon!"; pips dari `score_seat0/1` + `round_index`; `Text_Score` update. `OnGameOver` → `MatchContext.LastGameOver=msg; ToResult();`. `MatchWentAsyncReceived` TIDAK berlaku (RD forfeit-only) — jangan subscribe.
Test `ReflexDuelSceneTests.cs`: `ReflexDuel_HasPanelsAndPips` (path + 5 pips + refs), `ReflexDuel_GoPanelInactiveByDefault`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/ReflexDuel.unity` terdaftar; kedua test hijau.
- `ReflexDuelController.cs` memuat `PingIntervalSeconds = 2f` dan restore `15f` (grep dua-duanya).

## Task: Scene AirHockey (S6-AH)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene ReflexDuel (S6-RD)". Pola standar fase 2. Builder `AirHockeySceneBuilder.cs` → `Assets/Scenes/AirHockey.unity`; controller `AirHockeyController.cs`.
Koordinat: state server dalam ruang logis lebar 1080 (TDD §4.7) = 1:1 dengan canvas reference — mapping posisi = anchoredPosition pada `Panel_Table` full-width. Hierarki `UICanvas/Screen_AirHockey`: `Panel_Table` (stretch penuh area tengah, flat color + garis tengah Image tipis), `Img_Puck` (80x80 Knob sprite), `Img_MalletSelf` (140x140), `Img_MalletOpponent` (140x140), `Text_Score` ("3 — 2"), `Text_Timer` ("2:30", dari `time_remaining_ms`), `Banner_SuddenDeath` (inactive "SUDDEN DEATH!"), emote+connection via UiKit, `Text_Toast`.
AirHockeyController — field `SnapshotInterpolator puckInterp, opponentInterp` (ctor 0.05f, 0.1f). Input: `EventTrigger` Drag di Panel_Table (builder pasang) → konversi posisi pointer ke ruang logis, clamp ke setengah lapangan sendiri (seat 0 = bawah y<960; seat 1 = atas — tampilan di-MIRROR: pemain SELALU melihat dirinya di bawah; mapping mirror Y untuk seat 1 di SATU fungsi `ToViewSpace(Vector2 logical)` + inverse `ToLogicalSpace`), render mallet sendiri lokal langsung, kirim `SendRateLimited(Envelope{GameInput{AirHockeyInput{mallet_x, mallet_y}}}, "ah_input", 0.05f)`. `OnGameState` → parse `AirHockeyState`: `puckInterp.Push(pos, Time.unscaledTime)` dst; `Update()` render `Sample(Time.unscaledTime)`; mallet sendiri: kalau posisi server menyimpang >150 unit dari lokal → snap ke server (Global decisions #14); skor/timer/sudden_death update. `OnGameOver` → `LastGameOver` + `ToResult()`.
Test `AirHockeySceneTests.cs`: `AirHockey_HasTableAndActors`, `AirHockey_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/AirHockey.unity` terdaftar; kedua test hijau.
- `AirHockeyController.cs` memakai `SendRateLimited` dengan key `"ah_input"` dan `SnapshotInterpolator` (grep keduanya).

## Task: Scene WallDefense (S6-WD)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene AirHockey (S6-AH)". Pola standar fase 2. Builder `WallDefenseSceneBuilder.cs` → `Assets/Scenes/WallDefense.unity`; controller `WallDefenseController.cs`.
Orientasi: TDD §4.7 — KEDUA client render identik (gawang bersama di BAWAH, bola dari atas), TANPA mirror per seat; identitas via warna paddle + label "YOU" di paddle sendiri (`MatchContext.MySeat`).
Hierarki `UICanvas/Screen_WallDefense`: `Panel_Arena` (stretch tengah), `Img_Goal` (bar bawah full-width 40px), `Img_Paddle0` (240x50, warna biru) + `Img_Paddle1` (240x50, oranye) + `Text_YouMarker` (di-parent-kan runtime ke paddle milik `MySeat` saat Start), `Pool_Balls` container + `BallTemplate` (60x60 Knob, INACTIVE, SIBLING container — hukum populate), `Row_Lives` (5 Image hati placeholder kotak 48x48), `Text_Score`, `Banner_Wave` (inactive "Wave 3"), emote+connection, `Text_Toast`.
WallDefenseController — Input: drag horizontal → render paddle sendiri lokal + `SendRateLimited(WallDefenseInput{paddle_x}, "wd_input", 0.05f)`. `OnGameState` parse `WallDefenseState`: paddle lawan via `SnapshotInterpolator`; balls = sinkronisasi pool (clone/aktifkan sebanyak `balls.Count`, `SetActive(true)` setelah Instantiate, posisikan tiap bola — bola TANPA interpolasi per-bola cukup posisi langsung, jumlah berubah-ubah); lives → aktif/matikan hearts; `wave` berubah → tampilkan Banner_Wave 1.5s; score update. `OnGameOver` → `LastGameOver` + `ToResult()`; `MatchWentAsyncReceived` tidak berlaku (co-op end-run) — jangan subscribe.
Test `WallDefenseSceneTests.cs`: `WallDefense_BallTemplateIsInactiveSibling`, `WallDefense_HasFiveHearts`, `WallDefense_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/WallDefense.unity` terdaftar; 3 test hijau.
- `WallDefenseController.cs` TIDAK memuat mirror per-seat pada posisi arena (tidak ada fungsi flip Y/X berdasar seat; grep `MySeat` hanya untuk warna/label paddle).

## Task: Scene KeepUpDuo (S6-KU)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene WallDefense (S6-WD)". Pola standar fase 2. Builder `KeepUpDuoSceneBuilder.cs` → `Assets/Scenes/KeepUpDuo.unity`; controller `KeepUpDuoController.cs`. Kontrol = paddle-drag 1D (Global decisions #10 / TDD B1 default).
Hierarki `UICanvas/Screen_KeepUpDuo`: `Panel_Arena`, `Img_Floor` (bar bawah merah tipis — zona game over), `Img_Ball` (90x90 Knob), `Img_Paddle0`/`Img_Paddle1` (220x44, warna beda + `Text_YouMarker` pola WallDefense), `Glow_Turn0`/`Glow_Turn1` (Image 260x60 kuning transparan di bawah masing-masing paddle, inactive), `Text_Combo` (pusat atas, 80pt), emote+connection, `Text_Toast`.
KeepUpDuoController — drag → paddle sendiri lokal + `SendRateLimited(KeepUpDuoInput{paddle_x}, "ku_input", 0.05f)`. `OnGameState` parse `KeepUpDuoState`: bola via `SnapshotInterpolator`; paddle lawan interpolasi juga; `Text_Combo = score`; glow: `last_toucher_seat >= 0` → aktifkan glow di seat LAWAN dari last_toucher (dia yang wajib sentuh berikutnya — AC-KU-02), keduanya mati kalau -1; `game_over==true` → tunggu GameOver. `OnGameOver` → `LastGameOver` + `ToResult()`.
Test `KeepUpDuoSceneTests.cs`: `KeepUp_HasBallPaddlesGlows`, `KeepUp_GlowsInactiveByDefault`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/KeepUpDuo.unity` terdaftar; kedua test hijau.
- `KeepUpDuoController.cs` logika glow memakai `last_toucher_seat` (grep `LastToucherSeat`).

## Task: Scene Battleship (S6-BS)
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch. Depends on: "Scene KeepUpDuo (S6-KU)". Pola standar fase 2. Builder `BattleshipSceneBuilder.cs` → `Assets/Scenes/Battleship.unity`; controller `BattleshipController.cs` + `BattleshipGridView.cs` (file sendiri: MonoBehaviour dengan `[SerializeField] Image[] cells;` 100 sel via GridLayoutGroup 10x10 cell 64x64 + `[SerializeField] Button[] cellButtons;` — builder mengisi keduanya; method `public void SetCell(int row, int col, Color c)` dan `public event System.Action<int,int> CellTapped;`).
Interaksi placement per Global decisions #13 (tray-tap-rotate, BUKAN drag).
Hierarki `UICanvas/Screen_Battleship`: `Panel_Placement` (aktif default): `Grid_MyFleet` (BattleshipGridView), `Tray_Ships` (5 tombol `Btn_Ship5/4/3a/3b/2` label "5","4","3","3","2"), `Btn_Rotate` ("Rotate: Horizontal"), `Btn_Random`, `Btn_LockPlacement`, `Text_PlacementHint`; `Panel_Firing` (inactive): `Grid_Target` (BattleshipGridView kedua), `Grid_MyBoard` (BattleshipGridView ketiga, kecil 6x skala 0.5 pojok), `Text_TurnBanner` ("YOUR TURN"/"Their turn"), `Btn_Fire` (disabled sampai sel dipilih), `Text_ShotResult`; emote+connection, `Text_Toast`.
BattleshipController — Placement: pilih kapal dari tray (disable tombol kapal yang sudah ditaruh), tap sel = origin dengan orientasi toggle `Btn_Rotate`; setiap penempatan divalidasi `BattleshipPlacementValidator.CellsOccupied`-based pre-check (in-bounds + non-overlap; invalid → flash merah sel via coroutine 0.3s); `Btn_Random` → `BattleshipPlacementGenerator.Generate(System.Environment.TickCount)` render semua; `Btn_LockPlacement` (aktif hanya kalau `IsValid(current)` true) → `Send(GameInput{ BattleshipPlaceInput{ships} })`. Warna sel: abu=kosong, biru tua=kapal. `OnGameState` parse via `MatchContext`-seat: `BattleshipState` (state per-pemain dari server): `phase==BS_FIRING` → switch panel; `my_fleet` → Grid_MyBoard biru; `shots_against_me` → overlay merah(hit)/putih(miss) di Grid_MyBoard; `my_shots_result` → Grid_Target: abu=unshot, putih=miss, merah=hit, merah tua=sunk; `next_player_id` → banner + `Grid_Target` buttons interactable hanya saat giliranku; tap sel target (belum ditembak) → tandai crosshair (border kuning) + enable `Btn_Fire` → kirim `Send(GameInput{ BattleshipFireInput{row, col} })`; `my_ships_sunk_by_opponent` baru → `Text_ShotResult = "They sunk your {name}!"`. Resume async: `PendingResumeState` pola ConnectFour. `MatchWentAsyncReceived` → toast + ToHome (pola ConnectFour). `OnGameOver` → `LastGameOver` + `ToResult()`.
Test `BattleshipSceneTests.cs`: `Battleship_ThreeGridsEach100Cells` (3 BattleshipGridView, masing-masing cells.Length==100 semua non-null), `Battleship_FiringPanelInactiveByDefault`, `Battleship_ControllerRefsWired`.
**Acceptance criteria:**
- Standar pola scene fase 2; `Assets/Scenes/Battleship.unity` terdaftar; 3 test hijau.
- `BattleshipController.cs` memakai `BattleshipPlacementValidator.IsValid` sebelum kirim placement dan `BattleshipPlacementGenerator.Generate` untuk random (grep keduanya).

## Task: CHECKPOINT GATE VISUAL — semua scene terverifikasi mekanis
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch (gate). Depends on: SEMUA task fase 2. Tanpa perubahan kode kecuali fix ≤5 baris. Verifikasi:
1. EditMode suite penuh hijau, TERMASUK semua `*SceneTests` (Baseline, Home, Invite, Queue, Voting, Result, AsyncList, Profile, Shop, Settings, ReflexDuel, AirHockey, WallDefense, KeepUpDuo, Battleship — 15 kelas scene test).
2. `ProjectSettings/EditorBuildSettings.asset` memuat TEPAT 16 scene: Boot, Home, Invite, Queue, Voting, ConnectFour, ReflexDuel, AirHockey, WallDefense, KeepUpDuo, Battleship, Result, Profile, Shop, Settings, AsyncList — dan TIDAK memuat Lobby.
3. `grep -c "m_Script: {fileID: 0}" Assets/Scenes/*.unity` = 0 total.
4. Grep `GameObject.Find(` di `Assets/Scripts/UI/` = 0 hit (semua wiring serialized).
5. Tulis laporan gate ke `docs/gate-visual-report.md`: tabel scene × (file ada / test hijau / missing-script 0 / di build settings) + daftar hal yang perlu polish visual manusia (art pass menunggu design handoff). CATATAN MANUSIA di laporan: buka editor + jalankan `2UP → Build All` lalu spot-check hierarchy — opsional, bukan kriteria mesin.
**Acceptance criteria:**
- EditMode batchmode penuh exit 0, `failed="0"`, memuat ≥ 15 scene-test class.
- Keempat grep audit di atas menghasilkan angka yang disebut (0 untuk missing script & Find; 16 scene; 0 Lobby).
- `docs/gate-visual-report.md` ada dan memuat tabel per-scene lengkap.

---

## FASE 3 — Integrasi akhir (priority 3)

## Task: GATE AKHIR — APK build hijau
**Priority:** 3
**Parallel safe:** false
**Description:**
LOGIKA/batch (gate). Depends on: "CHECKPOINT GATE VISUAL". Jalankan build Android batchmode dari worktree: `"C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe" -batchmode -quit -projectPath . -buildTarget Android -executeMethod TwoUp.EditorTools.SkeletonBuilder.BuildApk -logFile .ccq-apk.log` (method existing; pastikan masih menunjuk daftar scene BARU — kalau `BuildApk` masih hardcode 3 scene lama, perbaiki agar memakai `EditorBuildSettings.scenes` yang enabled). APK keluar di `Builds/twoup-client.apk`. Kalau build gagal karena scene list → fix `BuildApk`; gagal karena hal lain → laporkan log 50 baris terakhir.
**Acceptance criteria:**
- `Builds/twoup-client.apk` ada dengan ukuran > 10 MB; log build memuat "APK build: Succeeded, errors=0".
- `BuildApk` memakai `EditorBuildSettings.scenes` (grep di `SkeletonBuilder.cs`), bukan daftar hardcode.
- EditMode suite penuh tetap hijau.

<!-- ccq:progress:start -->
## CCQ Progress — 15/32 selesai

- [x] `t_0716_0035_3d71df3c` Struktur asmdef + test scaffolding EditMode
- [x] `t_0716_0035_8ee99a99` Extend proto kontrak per TDD 3.1 + regen C#
- [x] `t_0716_0035_a2737e00` RateGate + extend NetworkClient (event baru, SendRateLimited, ping interval)
- [x] `t_0716_0035_7da194ac` GameCatalog ScriptableObject + asset seed
- [x] `t_0716_0035_2d08f139` Extend MatchContext + AppStateMachine (state & routing baru)
- [x] `t_0716_0035_7edc4605` Helper murni A - RoomCodeSanitizer, DeepLinkParser, InstallReferrerParser (+reader wrapper)
- [x] `t_0716_0035_5cd660fc` Helper murni B - formatter & sorter (voting, async list, reflex, ledger)
- [x] `t_0716_0035_c3a1cc80` SnapshotInterpolator untuk game real-time
- [x] `t_0716_0035_b215c6bd` Battleship placement - validator + generator deterministik
- [x] `t_0716_0035_8b549654` Platform plumbing - DeepLinkRouter, PushTokenClient, provider stub ads/IAP
- [x] `t_0716_0035_38edeebf` CHECKPOINT GATE LOGIKA - suite EditMode penuh hijau headless
- [x] `t_0716_0035_4e56267c` Refactor builder - UiKit + per-scene builder + SceneAsserts
- [x] `t_0716_0035_dd31e0c3` Shared HUD kit - EmoteWheelController + ConnectionIndicator + builder helper
- [x] `t_0716_0035_51124738` Scene Home (S2)
- [x] `t_0716_0035_cf1ecfeb` Extend scene Boot - App wiring baru + alur ke Home
- [ ] `t_0716_0035_59367503` Retire Lobby - hapus scene, controller, builder
- [ ] `t_0716_0035_1f3fd1bd` Scene InviteRoom (S3)
- [ ] `t_0716_0035_084b36c7` Scene Queue (S4)
- [ ] `t_0716_0035_5778be3a` Scene Voting (S5) + bot picker
- [ ] `t_0716_0035_d7fbd311` Scene Result (S7)
- [ ] `t_0716_0035_a70267c3` Scene AsyncMatches (S11)
- [ ] `t_0716_0035_f4706690` Scene Profile (S8)
- [ ] `t_0716_0035_b66d0de4` Scene Shop (S9)
- [ ] `t_0716_0035_a68f8c0d` Scene Settings (S10)
- [ ] `t_0716_0035_023a20a9` Extend scene ConnectFour - turn timer ring, emote wheel, jalur async
- [ ] `t_0716_0035_09be9778` Scene ReflexDuel (S6-RD)
- [ ] `t_0716_0035_dbdd042f` Scene AirHockey (S6-AH)
- [ ] `t_0716_0035_5e5491c1` Scene WallDefense (S6-WD)
- [ ] `t_0716_0035_a1a7f548` Scene KeepUpDuo (S6-KU)
- [ ] `t_0716_0035_a6c9041c` Scene Battleship (S6-BS)
- [ ] `t_0716_0035_c4cc2ec6` CHECKPOINT GATE VISUAL - semua scene terverifikasi mekanis
- [ ] `t_0716_0035_845163fb` GATE AKHIR - APK build hijau
<!-- ccq:progress:end -->
