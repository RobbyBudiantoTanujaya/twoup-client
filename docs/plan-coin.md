# Plan: 2UP client — retrofit ekonomi coin (GDD v1.2 / TDD v1.2) — untuk ccq

Sumber kontrak: `docs/TDD.md` v1.2 (§3.1.7-3.1.8 proto, §3.3 `EconomyState`, §5 scene retrofit, §7b test) dan `docs/GDD.md` v1.2 §4 — ADA DI REPO INI; task WAJIB membacanya untuk detail, bagian kritis tetap disalin inline per task.

**Konteks:** client MVP (35 task + Ping-echo) SUDAH selesai: 16 scene jadi, semua screen jalan. Plan ini RETROFIT ekonomi ticket → COIN: biaya main per game (tampil di kartu voting + tombol rematch), daily reward + login streak (popup di Home), rewarded ad = coin. Server punya plan retrofit sendiri (`twoup-server/docs/plan-coin.md`); client diuji headless tanpa server terhadap kontrak proto TDD §3.1.

**CATATAN UNTUK INGEST:** SEMUA task `live_editor: false` BY DESIGN — konvensi repo terkunci: scene digenerate **editor builder script** (`Assets/Editor/SceneBuilders/*.cs`) via batchmode `-executeMethod`, `.unity`+`.meta` hasilnya di-commit; worker TIDAK hand-edit YAML dan TIDAK pakai MCP live. Verifikasi hierarki = EditMode test `EditorSceneManager.OpenScene()` + assert.

## Global decisions (dikunci, worker tidak memutuskan ulang)

1. **Semua keputusan Global plan MVP tetap berlaku** (asmdef `TwoUp.Runtime`/`TwoUp.Editor`/`TwoUp.Tests.EditMode`, namespace logika murni `TwoUp.Logic`, function-over-form tanpa art baru, tidak ada package baru, server-authoritative, HUKUM SCRIPT: 1 MonoBehaviour/ScriptableObject = 1 file .cs nama file = nama class).
2. **Proto additive-only:** `proto/twoup.proto` di-extend PERSIS mengikuti `docs/TDD.md` §3.1.7-3.1.8 (field coin di ShopItem/ShopData/WalletUpdate, message `ClaimDailyReward`/`DailyRewardClaimed`/`EconomyConfig`, oneof 102-104). Field ticket lama TETAP ADA (tidak dihapus/rename), hanya tidak dibaca lagi. Regen via `tools/generate-protos.ps1`. File harus identik dengan server.
3. **Satu sumber saldo/harga di client = `TwoUp.EconomyState`** (static class, `Assets/Scripts/App/EconomyState.cs`, spec persis TDD §3.3): di-update HANYA oleh `NetworkClient` dari `EconomyConfig`/`WalletUpdate`; controller render dari sini + subscribe event `Changed`. TIDAK ADA angka harga/reward yang di-hardcode di client — server yang menentukan (client tidak tahu default server; sebelum `EconomyConfig` tiba, `CostOf` = 0 dan UI menampilkan harga hanya setelah data ada).
4. **Format tampilan coin (function-over-form, font TMP LiberationSans TANPA glyph emoji — DILARANG pakai emoji 🪙):** saldo = `"Coins: {n}"`; harga di kartu voting = `"{n}c"`; tombol rematch = `"Rematch ({n}c)"`; tombol ad = `"Watch Ad (+{n}c)"`; popup daily = header `"Daily Reward"`, baris streak `"Day {streak}"`, tombol claim `"Claim +{n}c"`.
5. **Aturan render affordability (server tetap enforce; client hanya hint):** kartu game S5 dengan `!EconomyState.CanAfford(gameId)` → `Button.interactable=false` + `Btn_GetCoins` kecil tampil di kartu; mode BotPicker (vs Bot) → SEMUA harga disembunyikan dan kartu selalu enabled (vs Bot gratis). `Btn_Rematch` S7: disabled + `Btn_GetCoins` saat `!CanAfford(gameId match barusan)`; match vs bot → label `"Rematch"` tanpa harga, selalu enabled.
6. **`Panel_GetCoins` (overlay bersama):** SATU controller `GetCoinsPanelController` + SATU builder helper statis `GetCoinsPanelBuilder.Add(Transform parent)` (file baru, BUKAN edit builder scene lain) yang dipanggil oleh builder scene Home/Voting/Result/Shop masing-masing. Isi panel: `Text_Headline` ("Not enough coins"), `Text_Balance` ("Coins: {n}"), `Btn_WatchAdShortcut` ("Watch Ad (+{n}c)") → kirim `ClaimAdTicket` via provider ads stub existing, `Btn_Close`. `ads_remaining_today == 0` → tombol ad disabled + `Text_Headline` = "No ads left. Come back tomorrow!". Panel default inactive; dibuka via `GetCoinsPanelController.Show()`.
7. **Daily reward popup S2:** auto-show SEKALI per app-session saat masuk Home dan `EconomyState.DailyRewardAvailable == true` (guard `static bool shownThisSession` di `HomeController` — reset saat app restart; sengaja sederhana). Claim → kirim `ClaimDailyReward`; begitu `DailyRewardClaimed` diterima, panel langsung `SetActive(false)` — feedback cukup saldo header yang berubah (function-over-form, tanpa animasi). Server balas `Error("daily_already_claimed")` → tutup panel diam-diam.
8. **Urutan pesan dari server (kontrak §4.10 server):** `EconomyConfig` lalu `WalletUpdate` tiba setelah `ServerHello` — `NetworkClient` cukup expose event; TIDAK ada logika urutan di client (state tahan menerima `WalletUpdate` sebelum `EconomyConfig`).
9. **Satu scene = satu owner task; semua task visual `parallel_safe: false`** (berbagi `EditorBuildSettings` + pola builder).

## Manual prerequisites (manusia, bukan task ccq)

- Tidak ada yang baru. (Ads asli tetap di belakang `TWOUP_ADS` — `Btn_WatchAdShortcut` memakai provider stub existing yang sama dengan S9.)
- Server retrofit (`twoup-server/docs/plan-coin.md`) untuk E2E; client plan ini tidak diblokir olehnya.

---

## Task: Extend proto client ekonomi coin per TDD §3.1.7-3.1.8 + regen C#
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA/batch. Edit `proto/twoup.proto` PERSIS mengikuti `docs/TDD.md` v1.2 §3.1.7-3.1.8 (baca file itu; konflik → TDD menang). ADDITIVE saja, field/message lama tidak diubah:
1. `ShopItem` + `int32 price_coins = 6;` `ShopData` + `int32 coin_balance = 5;` `WalletUpdate` + `int32 coin_balance = 4; int32 streak_count = 5; bool daily_reward_available = 6; int32 next_daily_reward_coins = 7;`
2. Message baru: `ClaimDailyReward {}`; `DailyRewardClaimed { int32 reward_coins = 1; int32 streak_count = 2; int32 coin_balance = 3; }`; `EconomyConfig { map<string, int32> game_coin_cost = 1; int32 ad_reward_coins = 2; int32 ad_daily_cap = 3; repeated int32 daily_streak_rewards = 4; int32 starting_balance = 5; }`.
3. Envelope oneof: `claim_daily_reward = 102; daily_reward_claimed = 103; economy_config = 104;` setelah `premium_purchased = 101`.
Regen: jalankan `tools/generate-protos.ps1` (auto-download protoc), commit proto + `Assets/Scripts/Generated/` + `.meta` baru bila ada. Extend test kontrak EditMode existing (atau buat `Assets/Tests/EditMode/EconomyContractTests.cs`, class `EconomyContractTests`, asmdef `TwoUp.Tests.EditMode`): `[Test] EconomyPayloads_RoundTrip()` — buat `Envelope` berisi `EconomyConfig` (`GameCoinCost["battleship"]=3`, `DailyStreakRewards` = {5,6,7,8,10,12,15}) → `ToByteArray()` → `Envelope.Parser.ParseFrom` → assert `PayloadCase == EconomyConfig` dan nilai utuh; ulangi untuk `DailyRewardClaimed{RewardCoins=7}` dan `WalletUpdate{CoinBalance=20, DailyRewardAvailable=true}`. `[Test] TicketFields_StillPresent()` — `new WalletUpdate().TicketBalance` compile & bernilai 0 (wire lama utuh).
**Acceptance criteria:**
- `proto/twoup.proto` memuat `price_coins`, `coin_balance`, `ClaimDailyReward`, `DailyRewardClaimed`, `EconomyConfig`, oneof 102-104 (grep) dan field lama utuh (grep `price_tickets = 3` dan `ticket_balance = 2`).
- `Assets/Scripts/Generated/` memuat type C# `EconomyConfig` dan `DailyRewardClaimed`.
- EditMode batchmode pass termasuk `EconomyContractTests.EconomyPayloads_RoundTrip` dan `TicketFields_StillPresent`; zero compile error.

## Task: EconomyState + event NetworkClient (EconomyConfig/WalletUpdate/DailyRewardClaimed)
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA/batch. Depends on: "Extend proto client ekonomi coin per TDD §3.1.7-3.1.8 + regen C#" — cek type `Twoup.V1.EconomyConfig` (namespace generated existing) ada; kalau belum, hentikan dan laporkan.
1. Buat `Assets/Scripts/App/EconomyState.cs` (asmdef `TwoUp.Runtime`, namespace `TwoUp`, static class murni — BUKAN MonoBehaviour) persis spec `docs/TDD.md` §3.3: properti `CoinBalance`, `StreakCount`, `DailyRewardAvailable`, `NextDailyRewardCoins`, `AdsRemainingToday` (int/bool), `GameCoinCost` (`IReadOnlyDictionary<string,int>`), `AdRewardCoins`; event `public static event System.Action Changed`; method `ApplyConfig(EconomyConfig cfg)` (salin map + AdRewardCoins, raise Changed), `ApplyWallet(WalletUpdate w)` (salin 5 field wallet, raise Changed), `CostOf(string gameId)` (0 kalau tak dikenal/config belum tiba), `CanAfford(string gameId)` (`CoinBalance >= CostOf`), dan `ResetForTests()` (null-kan semua — dipakai test).
2. `Assets/Scripts/Net/NetworkClient.cs`: tambah 3 event C# mengikuti pola event payload existing: `OnEconomyConfig(EconomyConfig)`, `OnWalletUpdate(WalletUpdate)`, `OnDailyRewardClaimed(DailyRewardClaimed)`; di switch dispatch `Envelope.PayloadCase` tambahkan ketiga case; SEBELUM raise event, panggil `EconomyState.ApplyConfig(...)` / `EconomyState.ApplyWallet(...)` (untuk `DailyRewardClaimed` juga update `EconomyState` via `ApplyWallet`-like: set `CoinBalance = msg.CoinBalance`, `StreakCount = msg.StreakCount`, `DailyRewardAvailable = false`, raise Changed — tambah method `ApplyDailyClaimed(DailyRewardClaimed d)` di EconomyState untuk ini). Tambah helper kirim: `SendClaimDailyReward()` (Envelope oneof `claim_daily_reward`) mengikuti pola `Send*` existing.
3. Test `Assets/Tests/EditMode/EconomyStateTests.cs` (class `EconomyStateTests`, `[SetUp]` panggil `EconomyState.ResetForTests()`): `CostOf_ReturnsConfigValue_UnknownGameZero` (ApplyConfig map {battleship:3} → CostOf("battleship")==3, CostOf("nope")==0, CostOf sebelum config==0), `CanAfford_ComparesBalanceToCost` (balance 2 via ApplyWallet, config {air_hockey:2, battleship:3} → CanAfford air_hockey true, battleship false), `ApplyWallet_RaisesChangedEvent` (counter naik tepat 1 per ApplyWallet), `ApplyDailyClaimed_UpdatesBalanceAndClearsAvailable` (setelah ApplyDailyClaimed{CoinBalance:25,StreakCount:2} → CoinBalance 25, DailyRewardAvailable false).
HUKUM SCRIPT: `EconomyState.cs` satu class per file.
**Acceptance criteria:**
- File `Assets/Scripts/App/EconomyState.cs` ada; `NetworkClient.cs` punya 3 event baru + `SendClaimDailyReward` (grep).
- EditMode batchmode pass termasuk 4 test `EconomyStateTests` (nama persis).
- Zero compile error; tidak ada scene yang berubah di task ini.

## Task: GetCoinsPanelController + builder helper GetCoinsPanelBuilder
**Priority:** 1
**Parallel safe:** false
**Description:**
LOGIKA+EDITOR/batch (belum menyentuh scene apa pun — scene di-rebuild oleh task per-scene berikutnya). Depends on: "EconomyState + event NetworkClient (...)" — cek `Assets/Scripts/App/EconomyState.cs` ada; kalau belum, hentikan dan laporkan.
1. `Assets/Scripts/UI/GetCoinsPanelController.cs` (MonoBehaviour, asmdef `TwoUp.Runtime`, namespace `TwoUp`): serialized fields `TMP_Text headline; TMP_Text balanceText; Button watchAdButton; TMP_Text watchAdLabel; Button closeButton;`. `Show()` → `gameObject.SetActive(true)` + `Refresh()`; `Hide()`; `OnEnable` subscribe `EconomyState.Changed` → `Refresh()` (unsubscribe di `OnDisable`). `Refresh()`: `balanceText.text = $"Coins: {EconomyState.CoinBalance}"`; `watchAdLabel.text = $"Watch Ad (+{EconomyState.AdRewardCoins}c)"`; kalau `EconomyState.AdsRemainingToday <= 0` → `watchAdButton.interactable=false`, `headline.text="No ads left. Come back tomorrow!"`, else `interactable=true`, `headline.text="Not enough coins"`. `watchAdButton.onClick` → panggil jalur klaim rewarded ad stub yang SAMA dengan tombol ad S9 (cari pemanggilan `ClaimAdTicket` existing di `ShopController`/ads provider dan pakai API yang sama — jangan duplikat logika provider); `closeButton.onClick` → `Hide()`.
2. `Assets/Editor/SceneBuilders/GetCoinsPanelBuilder.cs` (static class, asmdef `TwoUp.Editor`): method `public static GetCoinsPanelController Add(Transform uiCanvasRoot)` — bangun `Panel_GetCoins` (Image fullscreen dim semi-transparan + panel tengah `UISprite` sliced pola tombol existing, anchor center, 720x560) berisi `Text_Headline` (TMP), `Text_Balance` (TMP), `Btn_WatchAdShortcut` (pola `CreateButton()` helper existing) dengan child label TMP, `Btn_Close`; attach `GetCoinsPanelController`, isi semua serialized ref via `SetRef()`/`SerializedObject` pola builder existing, `SetActive(false)`. Return controller untuk di-wire caller.
3. Test `Assets/Tests/EditMode/GetCoinsPanelBuilderTests.cs`: `Add_BuildsPanelWithAllRefs` — buat GameObject root sementara di test, panggil `GetCoinsPanelBuilder.Add(root.transform)` → assert controller non-null, semua 5 serialized ref terisi (via `SerializedObject`), panel `activeSelf == false`, lalu destroy root.
HUKUM SCRIPT: `GetCoinsPanelController` satu file sendiri (builder static class bukan MonoBehaviour, bebas).
**Acceptance criteria:**
- Kedua file ada di path persis; `GetCoinsPanelController` = satu-satunya class di file-nya.
- EditMode batchmode pass termasuk `GetCoinsPanelBuilderTests.Add_BuildsPanelWithAllRefs`.
- Tidak ada file `.unity` yang berubah di task ini (grep git diff).

## Task: Retrofit Home — saldo coin + popup Daily Reward + Panel_GetCoins
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch via builder (konvensi repo: edit builder script → jalankan rebuild → commit `.unity`; JANGAN hand-edit YAML, JANGAN MCP). Depends on: "GetCoinsPanelController + builder helper GetCoinsPanelBuilder" — cek `Assets/Editor/SceneBuilders/GetCoinsPanelBuilder.cs` ada; kalau belum, hentikan dan laporkan. Scene owner: `Assets/Scenes/Home.unity` SAJA.
1. `Assets/Scripts/UI/HomeController.cs` (existing) tambah serialized fields: `TMP_Text coinBalanceText; GameObject dailyRewardPanel; TMP_Text dailyStreakText; TMP_Text dailyRewardAmountText; Button claimDailyButton;` plus ref `GetCoinsPanelController getCoinsPanel;`. Logika: `OnEnable` subscribe `EconomyState.Changed` → `RefreshCoin()` (`coinBalanceText.text = $"Coins: {EconomyState.CoinBalance}"`); auto-show popup daily SEKALI per app-session (`static bool dailyShownThisSession`): saat `EconomyState.DailyRewardAvailable` dan belum shown → `dailyRewardPanel.SetActive(true)`, `dailyStreakText.text = $"Day {EconomyState.StreakCount + 1}"`, `dailyRewardAmountText.text = $"Claim +{EconomyState.NextDailyRewardCoins}c"`, set flag. `claimDailyButton.onClick` → `NetworkClient.Instance.SendClaimDailyReward()`. Subscribe `NetworkClient.OnDailyRewardClaimed` → `dailyRewardPanel.SetActive(false)`; subscribe error `daily_already_claimed` (pola handler Error existing) → tutup panel diam-diam.
2. Builder `Assets/Editor/SceneBuilders/HomeSceneBuilder.cs` (existing) extend: `Text_CoinBalance` (TMP, anchor top-right header, teks default "Coins: 0"), `Panel_DailyReward` (panel modal tengah, pola panel existing: `Text_DailyHeader` "Daily Reward", `Text_StreakDay` ("Day 1"), `Btn_ClaimDaily` dengan label "Claim", default inactive), panggil `GetCoinsPanelBuilder.Add(...)` untuk `Panel_GetCoins`, wire SEMUA serialized ref baru `HomeController` via pola `SetRef()` existing. Jalankan rebuild scene Home via batchmode `-executeMethod` menu build existing (`2UP/Build All` equivalent method), commit `.unity`.
3. Test `Assets/Tests/EditMode/HomeEconomySceneTests.cs`: `Home_HasCoinEconomyObjects` — `EditorSceneManager.OpenScene("Assets/Scenes/Home.unity")` → assert ada `Text_CoinBalance` (TMP_Text), `Panel_DailyReward` inactive dengan child `Btn_ClaimDaily`, `Panel_GetCoins` inactive; `HomeController` serialized refs `coinBalanceText`/`dailyRewardPanel`/`claimDailyButton`/`getCoinsPanel` semuanya non-null (via `SerializedObject`).
HUKUM SCRIPT berlaku. Scene lain TIDAK disentuh.
**Acceptance criteria:**
- EditMode batchmode pass termasuk `HomeEconomySceneTests.Home_HasCoinEconomyObjects`.
- `git diff` scene hanya `Assets/Scenes/Home.unity` (+meta bila baru).
- `HomeController.cs` memuat `SendClaimDailyReward` dan `dailyShownThisSession` (grep).

## Task: Retrofit Voting — harga coin per kartu + disabled unaffordable + Panel_GetCoins
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch via builder. Depends on: "GetCoinsPanelController + builder helper GetCoinsPanelBuilder" (cek file builder helper ada; kalau belum, hentikan dan laporkan). Scene owner: `Assets/Scenes/Voting.unity` SAJA. Konteks: `VotingController` existing membangun 6 kartu game dari `GameCatalog.entries` (child template pola populate-from-template); mode `Panel_BotPicker` dipakai untuk vs Bot.
1. `VotingController.cs` extend: saat mengisi tiap kartu, tambah label harga `Text_CoinCost` (child template kartu) = `$"{EconomyState.CostOf(entry.gameId)}c"`; subscribe `EconomyState.Changed` → refresh semua kartu: mode voting normal → kartu `Button.interactable = EconomyState.CanAfford(gameId)`, dan `Btn_GetCoins` kecil di kartu `SetActive(!CanAfford)` → `getCoinsPanel.Show()`; mode BotPicker → `Text_CoinCost.gameObject.SetActive(false)` semua kartu + semua kartu enabled (vs Bot gratis, Global decision #5). `EconomyState.CostOf==0` (config belum tiba/game gratis) → label "0c" disembunyikan (`SetActive(false)`), kartu enabled.
2. Builder `VotingSceneBuilder.cs` extend: template kartu dapat child `Text_CoinCost` (TMP kecil pojok kanan-bawah kartu) + `Btn_GetCoins` (tombol kecil, label "+c", default inactive); `GetCoinsPanelBuilder.Add(...)` untuk `Panel_GetCoins`; wire ref `getCoinsPanel` di `VotingController`. Rebuild scene Voting batchmode, commit `.unity`.
3. Test `Assets/Tests/EditMode/VotingEconomySceneTests.cs`: `Voting_CardTemplateHasCostAndGetCoins` — OpenScene Voting → template kartu punya child `Text_CoinCost` (TMP_Text) dan `Btn_GetCoins`; `Panel_GetCoins` ada + inactive; ref `getCoinsPanel` non-null. Tambah test logika murni di `EconomyStateTests` TIDAK perlu (sudah ada); logika interactable dicek lewat method testable: refactor kecil — `public static bool CardEnabled(string gameId, bool botPickerMode) => botPickerMode || EconomyState.CanAfford(gameId);` di `VotingController` (static, murni) + test `VotingCardRuleTests.cs`: `CardEnabled_BotPickerAlwaysTrue`, `CardEnabled_FollowsAffordability` (set EconomyState via ApplyConfig/ApplyWallet).
**Acceptance criteria:**
- EditMode batchmode pass termasuk `VotingEconomySceneTests.Voting_CardTemplateHasCostAndGetCoins`, `VotingCardRuleTests.CardEnabled_BotPickerAlwaysTrue`, `VotingCardRuleTests.CardEnabled_FollowsAffordability`.
- `git diff` scene hanya `Assets/Scenes/Voting.unity`.
- `VotingController.cs` memuat `CardEnabled` static method (grep).

## Task: Retrofit Result — harga di Btn_Rematch + disabled + Panel_GetCoins
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch via builder. Depends on: "GetCoinsPanelController + builder helper GetCoinsPanelBuilder" (cek file ada; kalau belum, hentikan dan laporkan). Scene owner: `Assets/Scenes/Result.unity` SAJA. Konteks: `ResultController` existing tahu `game_id` match barusan (dari `MatchContext`/state existing) dan tahu apakah lawan bot (jalur `StartBotMatch` — cari flag/konteks existing; kalau tidak ada flag eksplisit, tambah `public static bool LastMatchVsBot` di `MatchContext` yang di-set true di jalur kirim `StartBotMatch` dan false saat `PairFound` — dua titik itu saja).
1. `ResultController.cs` extend: label rematch — vs bot → `"Rematch"`, selain itu `$"Rematch ({EconomyState.CostOf(gameId)}c)"` (cost 0 → `"Rematch"` polos); subscribe `EconomyState.Changed` → `rematchButton.interactable = vsBot || EconomyState.CanAfford(gameId)`; `Btn_GetCoins` kecil di sebelah tombol `SetActive(!interactable)` → `getCoinsPanel.Show()`. Logika testable statis: `public static string RematchLabel(string gameId, bool vsBot)` dan `public static bool RematchEnabled(string gameId, bool vsBot)` (murni, baca EconomyState).
2. Builder `ResultSceneBuilder.cs` extend: `Btn_GetCoins` kecil sebelah `Btn_Rematch` (default inactive), `GetCoinsPanelBuilder.Add(...)`, wire ref. Rebuild scene Result batchmode, commit.
3. Test: `Assets/Tests/EditMode/ResultEconomySceneTests.cs` `Result_HasGetCoinsObjects` (OpenScene → `Btn_GetCoins` ada inactive, `Panel_GetCoins` ada inactive, ref non-null); `ResultRematchRuleTests.cs`: `RematchLabel_VsBotPlain_HumanShowsCost` (config {air_hockey:2} → label "Rematch (2c)" human, "Rematch" bot), `RematchEnabled_FollowsAffordabilityUnlessBot` (balance 1, cost 2 → false human, true bot).
**Acceptance criteria:**
- EditMode batchmode pass termasuk 3 test baru di atas (nama persis).
- `git diff` scene hanya `Assets/Scenes/Result.unity`.
- `MatchContext` memuat `LastMatchVsBot` yang di-set di jalur `StartBotMatch` dan `PairFound` (grep 2 call site).

## Task: Retrofit Shop — saldo coin + harga price_coins + label Watch Ad
**Priority:** 2
**Parallel safe:** false
**Description:**
VISUAL/batch via builder. Depends on: "GetCoinsPanelController + builder helper GetCoinsPanelBuilder" (cek file ada; kalau belum, hentikan dan laporkan). Scene owner: `Assets/Scenes/Shop.unity` SAJA. Konteks: `ShopController` existing membaca `ShopData.ticket_balance` dan `ShopItem.price_tickets` — itu yang diganti.
1. `ShopController.cs`: baca `ShopData.coin_balance` dan `ShopItem.price_coins` (field proto baru; field ticket lama JANGAN dibaca lagi — grep nol `TicketBalance`/`PriceTickets` di file ini setelah selesai); nama GameObject/serialized field `Text_TicketBalance` di-rename `Text_CoinBalance` (builder + controller + `SetRef`); format `"Coins: {n}"`; sinkron juga dengan `EconomyState` (subscribe `Changed` → refresh saldo, karena `WalletUpdate` bisa datang dari luar shop). Label tombol ad = `$"Watch Ad (+{EconomyState.AdRewardCoins}c)"` di-refresh saat `Changed`. Saat `PurchaseItem` ditolak `Error("insufficient_coins")` → `getCoinsPanel.Show()` (shortcut item terkunci, GDD v1.2 §4.3).
2. Builder `ShopSceneBuilder.cs`: rename objek `Text_TicketBalance` → `Text_CoinBalance` (builder yang menciptakannya — ganti di kode builder), label default "Coins: 0"; `GetCoinsPanelBuilder.Add(...)` untuk `Panel_GetCoins` (konsisten TDD §5: panel ada di Home/Voting/Result/Shop) + wire ref `getCoinsPanel` di `ShopController`. Rebuild scene Shop batchmode, commit.
3. Test `Assets/Tests/EditMode/ShopEconomySceneTests.cs`: `Shop_CoinBalanceObjectExists_TicketGone` — OpenScene Shop → ada `Text_CoinBalance` (TMP_Text), TIDAK ada GameObject bernama `Text_TicketBalance`; `Panel_GetCoins` ada + inactive; ref saldo & `getCoinsPanel` di `ShopController` non-null.
**Acceptance criteria:**
- EditMode batchmode pass termasuk `ShopEconomySceneTests.Shop_CoinBalanceObjectExists_TicketGone`.
- Grep nol `TicketBalance` dan `PriceTickets` di `Assets/Scripts/UI/ShopController.cs` dan `Assets/Editor/SceneBuilders/ShopSceneBuilder.cs`.
- `git diff` scene hanya `Assets/Scenes/Shop.unity`.

## Task: GATE ekonomi coin client — rebuild semua + audit
**Priority:** 3
**Parallel safe:** false
**Description:**
Gate audit, TANPA fitur baru. Depends on: keempat task retrofit scene (Home/Voting/Result/Shop) selesai — cek keempat file test scene economy ada; kalau belum, hentikan dan laporkan.
1. Jalankan full rebuild semua scene via batchmode `-executeMethod` menu build existing → tidak ada error; commit perubahan scene bila builder menghasilkan diff (harusnya idempotent — diff selain timestamp = selidiki dan perbaiki builder).
2. EditMode batchmode FULL hijau (semua test lama + semua test plan ini).
3. Grep audit: (a) nol `TicketBalance`/`PriceTickets` di `Assets/Scripts/` KECUALI `Assets/Scripts/Generated/` (kode generated memang memuat field lama — itu benar); (b) nol emoji `🪙` di `Assets/`; (c) `EconomyState` dirujuk oleh `HomeController`, `VotingController`, `ResultController`, `ShopController`, `GetCoinsPanelController` (grep ≥1 per file); (d) tidak ada TODO/FIXME baru di file yang diubah plan ini.
4. Smoke boot: batchmode buka scene `Boot.unity` via EditMode test existing pattern (kalau ada test boot existing cukup pastikan hijau) — tidak ada `MissingReferenceException`/missing script di log untuk kelima scene retrofit (`Home`, `Voting`, `Result`, `Shop`, `Boot`): tulis test `Assets/Tests/EditMode/EconomyGateTests.cs` `AllRetrofitScenes_NoMissingScripts` — loop OpenScene 4 scene retrofit → `GameObject.FindObjectsOfType<MonoBehaviour>(true)` tidak ada yang null (missing script check via `GetComponents<Component>()` mengandung null).
**Acceptance criteria:**
- EditMode batchmode FULL pass termasuk `EconomyGateTests.AllRetrofitScenes_NoMissingScripts`.
- Semua grep poin 3 terpenuhi.
- Rebuild scene idempotent (menjalankan build dua kali → `git status` bersih setelah run kedua).

---

<!-- ccq:progress:start -->
## CCQ Progress — 3/8 selesai

- [x] `t_0717_1950_b0347701` Extend proto client ekonomi coin (TDD §3.1.7-3.1.8) + regen C#
- [x] `t_0717_1950_18e81c41` EconomyState static class + NetworkClient economy events
- [x] `t_0717_1950_a143f9ce` GetCoinsPanelController + GetCoinsPanelBuilder shared overlay helper
- [ ] `t_0717_1950_010706b9` Retrofit Home scene: coin balance + Daily Reward popup + Panel_GetCoins
- [ ] `t_0717_1950_36cb426f` Retrofit Voting scene: per-card coin price + affordability disable + Panel_GetCoins
- [ ] `t_0717_1950_7c7e4451` Retrofit Result scene: coin price on Btn_Rematch + affordability disable + Panel_GetCoins
- [ ] `t_0717_1950_4ea3e0d6` Retrofit Shop scene: coin balance + price_coins + Watch Ad label
- [ ] `t_0717_1950_bb08f418` GATE: coin economy client — full rebuild + audit
<!-- ccq:progress:end -->
