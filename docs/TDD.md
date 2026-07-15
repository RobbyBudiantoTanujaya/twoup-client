# TDD: 2UP — full roster (6 games) on top of the walking skeleton

## 1. Referensi

- GDD: `docs/GDD.md` v1 (2026-07-15)
- Asset list: `docs/asset-list.md` v1 (2026-07-15)
- Handoff: **belum ada.** GDD §10 sengaja menitipkan beberapa keputusan (domain landing page, kontrol Keep-Up Duo, orientasi kamera Wall Defense) ke "fase design" yang belum berjalan. Ini bukan input yang hilang — GDD sendiri menyatakan design phase belum terjadi. Item yang genuinely butuh fase itu ditandai `[DECIDED: default sementara]` + dicatat di Section 11 (Blockers).
- Repo state: dibaca langsung dari working tree, bukan file repo-state.md terpisah (kedua repo sudah clone lokal, konfirmasi in-sync dengan origin/main sebelum membaca):
  - Server: `https://github.com/RobbyBudiantoTanujaya/twoup-server` @ `10cc70b` (branch main)
  - Client: `https://github.com/RobbyBudiantoTanujaya/twoup-client` @ `e1ccd0a` (branch main)

**Catatan cakupan dokumen ini:** ini adalah TDD gabungan server (Go) + client (Unity/C#), karena hampir seluruh isi yang GDD §9 serahkan ke TDD (netcode, proto, MySQL, bot, reconnect, invite backend, metrik) adalah pekerjaan server. Section 9 menandai eksplisit bagian mana yang jadi input `ccq-plan-unity` (client saja) vs bagian yang butuh rencana Go terpisah.

---

## 2. Keputusan arsitektur global

- **Unity version:** 6000.0.75f1 (dari `ProjectVersion.txt`, tidak berubah).
- **Go version:** 1.24.0 (dari `go.mod`, tidak berubah).
- **Pola client:** thin MonoBehaviour controllers per scene, server-authoritative, `NetworkClient` singleton isolasi socket, `AppStateMachine` isolasi scene flow — semua tidak berubah (CLAUDE.md client, hard rules 1-4).
- **Pola server:** actor goroutine per match (`match.Runner`), `Game`/`Bot` interface generik di `internal/game`, input manusia+bot lewat `Runner.HandleInput` yang sama, MySQL tanpa ORM — semua tidak berubah (CLAUDE.md server, rules 1-4).
- **Pemisahan lapisan (client):** logika = C# murni headless-testable (helper class baru, lihat §7); visual = scene di-author via `Assets/Editor/SkeletonBuilder.cs` (pola existing, JANGAN construct UI runtime, JANGAN hand-edit `.unity`/`.prefab`).
- **Konvensi existing yang wajib dipatuhi:**
  - Proto: satu file `proto/twoup/v1/messages.proto` (server) / `proto/twoup.proto` (client, isi identik), regenerate via `make proto` (server, buf) / `tools/generate-protos.ps1` (client, protoc mentah) — dua jalur regen independen dari satu sumber, sudah berjalan, tidak diubah.
  - Coordinate space: 1080×1920 (reference resolution `CanvasScaler` client, sudah dipakai di `SkeletonBuilder.cs`). **Keputusan baru:** semua physics real-time (Air Hockey, Wall Defense, Keep-Up Duo) server men-simulasikan dalam unit logis yang SAMA (lebar arena = 1080 unit), supaya tidak ada layer konversi antara physics server dan render client.
  - Game id string (dipakai di proto `game_id` field, `game.Register` di server, `matches.game_id` MySQL, `GameCatalog` client) — **keputusan baru, dipakai di semua tempat lain di dokumen ini:**
    ```
    connect_four   (sudah ada)
    reflex_duel
    air_hockey
    wall_defense
    keepup_duo
    battleship
    ```

### Apa yang BERUBAH dari skeleton (transparansi, bukan "jangan diubah")

Instruksi mengunci **pola** (actor goroutine, `Runner.HandleInput`, `PlayerInput` tunggal manusia+bot, `Game`/`Bot` interface, proto sebagai source of truth) — bukan berarti nol file server berubah. File yang paling banyak berubah dan alasannya:

| File | Perubahan | Kenapa |
|------|-----------|--------|
| `internal/match/matchmaker.go`, `rooms.go` | Pairing tidak lagi langsung `StartMatch`; hanya membentuk pasangan lalu menyerahkan ke `Pairing` actor baru (voting) | GDD 6.1 butuh voting antara pairing dan game start; skeleton tidak punya ini sama sekali |
| `internal/match/runner.go` | `Service.StartMatch` menerima `gameID` sebagai parameter (bukan field `Service.GameID` tetap); keputusan rematch jadi 3-state (`REMATCH_SAME_GAME`/`NEXT_GAME`/leave) bukan bool | Server harus melayani 6 game dari satu `Service`; GDD 6.2 butuh "Next Game" yang skeleton tidak punya |
| `internal/game/game.go` | Dua interface opsional baru: `PerPlayerGame` (fog-of-war Battleship) dan `Resumable` (hibernasi match async) | Battleship butuh state berbeda per pemain; C4/BS async harus lolos restart server — base `Game` interface TIDAK berubah, existing 1 game (`connect_four`) TIDAK perlu implement keduanya |
| `internal/server/session.go` | State machine sesi bertambah (`statusVoting`, dst.), reconnect resume path baru | GDD butuh voting + reconnect; skeleton eksplisit `TODO(reconnection)` di file ini |
| `internal/store/mysql.go`, `store.go`, `memory.go` | +11 tabel baru, +9 kolom di `players`, +2 kolom di `matches`, +1 nilai enum di `match_players.result`; `Store` interface di-extend (§4.8) — kedua implementasi (MySQL & memory) | Item 2 |

Yang **tidak** berubah sama sekali: `game.Game`/`game.Bot` base interface, `Runner.HandleInput`/`HandleRematch`(signature lama tetap ada, hanya bertambah makna)/`HandleLeave`, `Participant` interface, `connectfour` package (kecuali menambah `LoadState` — additive), pola `botParticipant` menerima envelope lewat `Send()` yang sama dengan client.

---

## 3. Data schema

### 3.1 Proto v0 — extend, tidak dirombak

Field/message v0 yang sudah ada **tidak ada yang diubah tipe atau nomornya** (lolos `buf breaking` lint `WIRE_JSON` yang sudah aktif di `buf.yaml`). Semua di bawah ini murni additive: field baru di message existing, message baru, oneof entry baru.

#### 3.1.1 Field baru di message v0 (additive)

```protobuf
message RoomCreated {
  string room_code = 1;
  int64 expires_at_unix_ms = 2;   // NEW — TTL reservasi, lihat §3.1.4
}

message GameResult {
  repeated string winner_player_ids = 1;
  bool draw = 2;
  int32 co_op_score = 3;      // NEW — skor bersama untuk game co-op (WD, KU); 0 untuk game versus
  bool co_op_new_best = 4;    // NEW — true kalau co_op_score memecahkan PairDuoScore.best_score pasangan ini
}

message RematchRequest {
  string match_id = 1;
  bool accept = 2;             // v0: false = decline/leave (perilaku tidak berubah)
  RematchChoice choice = 3;    // NEW — default 0 = REMATCH_SAME_GAME, cocok dengan makna v0 accept=true
}
enum RematchChoice {
  REMATCH_SAME_GAME = 0;
  NEXT_GAME = 1;
}
```

#### 3.1.2 Voting (item 3) — Envelope oneof field 60-67

```protobuf
message Envelope { oneof payload {
  // ... field 1-50 v0 tidak berubah ...
  PairFound        pair_found         = 60;
  VoteGame         vote_game          = 61;
  VoteUpdate       vote_update        = 62;
  VotingLocked     voting_locked      = 63;
  VotingShowdown   voting_showdown    = 64;
  ShowdownPick     showdown_pick      = 65;
  VotingCancelled  voting_cancelled   = 66;
  StartBotMatch    start_bot_match    = 67;
  RematchStatusUpdate rematch_status_update = 31; // adjacent ke RematchRequest=30, bukan blok voting
  RoomJoinPending   room_join_pending  = 70;
  RoomExpired       room_expired       = 71;
  ListAsyncMatches  list_async_matches = 72;
  AsyncMatchList    async_match_list   = 73;
  ResumeAsyncMatch  resume_async_match = 74;
  MatchResumed      match_resumed      = 75;
  MatchWentAsync    match_went_async   = 76;
  EmoteSend         emote_send         = 80;
  EmoteBroadcast    emote_broadcast    = 81;
  // Meta-systems (Profile/Shop/Settings/push) — §3.1.7
  GetProfile        get_profile        = 90;
  ProfileData       profile_data       = 91;
  GetPairDetail     get_pair_detail    = 92;
  PairDetail        pair_detail        = 93;
  GetShop           get_shop           = 94;
  ShopData          shop_data          = 95;
  ClaimAdTicket     claim_ad_ticket    = 96;
  WalletUpdate      wallet_update      = 97;
  PurchaseItem      purchase_item      = 98;
  SetProfile        set_profile        = 99;
  RegisterPushToken register_push_token = 100;
  PremiumPurchased  premium_purchased  = 101;
}}

// Vs Bot (GDD S2/S5, mode eksplisit): melewati Pairing/voting sepenuhnya —
// server pilih bot random dari bot_profile dengan tier+game tsb, langsung
// Service.StartMatch. Balasan sukses = MatchFound+GameStart seperti biasa.
message StartBotMatch {
  string game_id = 1;
  string tier = 2;      // "easy" | "medium" | "hard"
}

// Dikirim ke participant yang MASIH terhubung saat match live dikonversi ke
// async (grace habis, §6.4) — client keluar dari scene game, kembali ke Home,
// badge S11 bertambah.
message MatchWentAsync { string match_id = 1; string game_id = 2; }

// Dikirim server->kedua pemain begitu pairing terbentuk (queue match atau room
// join), SEBELUM game dipilih. Menggantikan MatchFound sebagai sinyal
// "pairing terbentuk" — MatchFound v0 sekarang dikirim setelah COUNTDOWN
// selesai, di dalam startGame() (§4.2), bukan saat pairing.
// Membawa data badge pasangan untuk header S5 (GDD 6.1: milestone/duo level
// tampil di atas grid voting).
message PairFound {
  string pair_id = 1;
  repeated PlayerInfo players = 2;
  string milestone_tier = 3;   // none|rivals|arch_rivals|nemesis (baris agregat pair_ledger)
  int32 duo_level = 4;         // 1-5 dari total co-op match pasangan
  int32 total_matches = 5;     // agregat versus+co-op
}
message VoteGame { string pair_id = 1; string game_id = 2; }               // client -> server
message VoteUpdate {
  string pair_id = 1;
  map<string, string> votes_by_player_id = 2;  // player_id -> game_id, live reflect (GDD 6.1.1)
}
message VotingLocked {
  string pair_id = 1;
  string game_id = 2;
  int32 countdown_ms = 3;   // 3000, client menghitung mundur lokal (lihat §6.1)
}
message VotingShowdown {
  string pair_id = 1;
  repeated string candidate_game_ids = 2;  // selalu 2 entri
  int64 deadline_unix_ms = 3;
}
message ShowdownPick { string pair_id = 1; string game_id = 2; }           // client -> server, tap kartu lawan
message VotingCancelled { string pair_id = 1; string reason = 2; }         // reason: "opponent_left"
message RematchStatusUpdate {
  string match_id = 1;
  string player_id = 2;
  RematchChoice choice = 3;
}
```

#### 3.1.3 Async + push (item 5)

```protobuf
message ListAsyncMatches {}   // client -> server, buka S11
message AsyncMatchSummary {
  string match_id = 1;
  string game_id = 2;
  string opponent_player_id = 3;
  string opponent_display_name = 4;
  bool your_turn = 5;
  int64 forfeit_deadline_unix_ms = 6;
}
message AsyncMatchList { repeated AsyncMatchSummary matches = 1; }         // server -> client
message ResumeAsyncMatch { string match_id = 1; }                          // client -> server, tap match di S11 atau deep-link dari push
message MatchResumed {                                                     // server -> client yang resume/reconnect
  string match_id = 1;
  string game_id = 2;
  bytes state = 3;              // GameState.state format sama seperti biasa
  int32 grace_seconds_remaining = 4;  // untuk resume live disconnect; 0 kalau resume dari async cold
}
```

#### 3.1.4 Room / invite (item 6)

```protobuf
message RoomJoinPending { string room_code = 1; string invitee_display_name = 2; }  // -> host, live ATAU trigger FCM invitee_joined kalau host offline
message RoomExpired { string room_code = 1; }
```

`Room` (server) state machine dan TTL — lihat §6.3.

#### 3.1.5 Emote (disebut GDD §7 preamble, belum ada di v0 sama sekali)

```protobuf
message EmoteSend      { string match_id = 1; string emote_id = 2; }               // client -> server
message EmoteBroadcast { string match_id = 1; string player_id = 2; string emote_id = 3; }  // server -> keduanya
```
`emote_id` = key dari asset-list §1.3 (`emote_thumbsup`, `emote_lol`, `emote_wow`, `emote_cry`, `emote_fire`, `emote_gg`, plus 16 `emote_pack_XX_N`). Cooldown 3 detik per `(match_id, player_id)`, ditolak senyap (tidak broadcast, tidak error — GDD tidak minta feedback error untuk ini) kalau melanggar.

#### 3.1.6 Per-game payload — 5 game baru

Semua pesan ini BUKAN anggota `Envelope` oneof (sama seperti `ConnectFourInput`/`ConnectFourState` v0) — mereka di-marshal ke `GameInput.input` / `GameState.state` bytes oleh masing-masing `Game` implementation, sesuai pola yang sudah ada.

```protobuf
// --- Reflex Duel ---
// Tap sengaja tidak membawa timestamp: adjudikasi memakai waktu terima server
// + kompensasi RTT/2 (§4.7); clock client tidak bisa dipercaya.
message ReflexDuelInput {}
enum ReflexRoundPhase { RD_WAITING = 0; RD_ARMED = 1; RD_RESOLVED = 2; }
message ReflexDuelState {
  ReflexRoundPhase phase = 1;
  int32 round_index = 2;              // 0-4 (best-of-5)
  int32 score_seat0 = 3;
  int32 score_seat1 = 4;
  int64 go_server_ts_ms = 5;          // 0 kalau belum ARMED
  int32 last_round_winner_seat = 6;   // -1 = belum ada round selesai
  int32 last_round_ms_seat0 = 7;
  int32 last_round_ms_seat1 = 8;
  bool last_round_false_start_seat0 = 9;
  bool last_round_false_start_seat1 = 10;
}

// --- Air Hockey ---
message AirHockeyInput { float mallet_x = 1; float mallet_y = 2; }   // koordinat logis, 0-1080 lebar meja
message AirHockeyState {
  float puck_x = 1; float puck_y = 2; float puck_vx = 3; float puck_vy = 4;
  float mallet_seat0_x = 5; float mallet_seat0_y = 6;
  float mallet_seat1_x = 7; float mallet_seat1_y = 8;
  int32 score_seat0 = 9; int32 score_seat1 = 10;
  int32 time_remaining_ms = 11;
  bool sudden_death = 12;
}

// --- Wall Defense ---
message WallDefenseInput { float paddle_x = 1; }   // paddle geser 1 sumbu di depan gawang sendiri
message WDBall { float x = 1; float y = 2; float vx = 3; float vy = 4; int32 target_seat = 5; }
message WallDefenseState {
  float paddle_seat0_x = 1;
  float paddle_seat1_x = 2;
  repeated WDBall balls = 3;
  int32 lives = 4;
  int32 wave = 5;
  int32 score = 6;
  bool wave_transition = 7;
}

// --- Keep-Up Duo ---
// [DECIDED: kontrol paddle-drag dipilih sebagai default engineering — lihat
// Blocker B1 di §11, kontrol final tunduk pada fase design.]
message KeepUpDuoInput { float paddle_x = 1; }
message KeepUpDuoState {
  float ball_x = 1; float ball_y = 2; float ball_vx = 3; float ball_vy = 4;
  float paddle_seat0_x = 5;
  float paddle_seat1_x = 6;
  int32 score = 7;
  int32 last_toucher_seat = 8;   // -1 = belum ada sentuhan; aturan alternasi AC-KU-02 dicek server dari field ini
  bool game_over = 9;
}

// --- Battleship ---
message ShipPlacement { int32 length = 1; int32 row = 2; int32 col = 3; bool horizontal = 4; }
message BattleshipPlaceInput { repeated ShipPlacement ships = 1; }   // 5 entri: length 5,4,3,3,2
message BattleshipFireInput  { int32 row = 1; int32 col = 2; }
enum BattleshipPhase { BS_PLACEMENT = 0; BS_FIRING = 1; BS_FINISHED = 2; }
// State PER-PEMAIN (lihat game.PerPlayerGame §4.2) — fog of war: my_ships hanya
// terisi untuk pemilik state ini, opponent_shots_result tidak membocorkan
// posisi kapal opponent yang belum kena.
message BattleshipState {
  BattleshipPhase phase = 1;
  bool my_placement_locked = 2;
  bool opponent_placement_locked = 3;
  repeated int32 my_shots_result = 4;        // 100 sel row-major, tembakan SAYA ke opponent: 0 unshot/1 miss/2 hit/3 sunk
  repeated int32 shots_against_me = 5;       // 100 sel, tembakan opponent ke saya, encoding sama
  repeated int32 my_fleet = 6;               // 100 sel, layout armada SAYA: 0 kosong / 1-5 = ship id, HANYA di state milik saya
  string next_player_id = 7;                 // hanya valid saat phase=BS_FIRING
  repeated string my_ships_sunk_by_opponent = 8;  // nama kapal SAYA yang sudah tenggelam (opponent lihat ini via shots_against_me)
}
```

#### 3.1.7 Meta-systems: Profile/Ledger, Shop/Wallet, Settings, push token

Kontrak untuk S8/S9/S10 (satu-satunya kanal adalah WebSocket yang sama; tidak ada REST untuk client).

```protobuf
// --- Profile & ledger (S8) ---
message GetProfile {}   // client -> server; jawaban: ProfileData
message ProfileData {
  PlayerInfo me = 1;
  string avatar_id = 2;            // key asset-list, mis. "avatar_preset_03"
  string frame_id = 3;             // key asset-list, mis. "frame_default"
  bool premium = 4;
  int32 bot_wins = 5;              // stat pribadi vs bot (GDD 5.1)
  int32 bot_losses = 6;
  int32 bot_draws = 7;
  repeated PairSummary pairs = 8;  // daftar rivalry, urut last_played_at desc
}
message PairSummary {
  string other_player_id = 1;
  string other_display_name = 2;
  string other_avatar_id = 3;
  int32 wins_mine = 4;             // baris agregat all-games, dari sudut pandang peminta
  int32 wins_theirs = 5;
  int32 draws = 6;
  int32 total_matches = 7;         // versus+co-op (basis milestone)
  string milestone_tier = 8;       // none|rivals|arch_rivals|nemesis
  int32 duo_level = 9;             // 1-5
  int64 last_played_at_unix_ms = 10;
}
message GetPairDetail { string other_player_id = 1; }   // tap pasangan di S8
message PairDetail {
  PairSummary summary = 1;
  repeated PairGameLine versus_lines = 2;
  repeated DuoGameLine coop_lines = 3;
  string streak_holder_player_id = 4;
  int32 streak_count = 5;
}
message PairGameLine { string game_id = 1; int32 wins_mine = 2; int32 wins_theirs = 3; int32 draws = 4; }
message DuoGameLine  { string game_id = 1; int32 best_score = 2; int32 total_matches = 3; }

// Edit profil (GDD §3: display_name editable; equip avatar/frame dari inventory).
// Field kosong = tidak diubah. Jawaban: ProfileData terbaru, atau Error
// ("item_not_owned" untuk equip yang tidak dimiliki).
message SetProfile { string display_name = 1; string avatar_id = 2; string frame_id = 3; }

// --- Shop & wallet (S9) ---
message GetShop {}   // jawaban: ShopData
message ShopItem {
  string item_key = 1;             // = cosmetic_item.item_key = key asset-list
  string type = 2;                 // frame|emote_pack|board_skin|victory_anim
  int32 price_tickets = 3;
  bool premium_exclusive = 4;
  bool owned = 5;
}
message ShopData {
  repeated ShopItem items = 1;     // 19 item + default
  int32 ticket_balance = 2;
  int32 ads_remaining_today = 3;   // 5 - daily_ad_count
  bool premium = 4;
}
// MVP: klaim dipercaya dari client setelah rewarded ad selesai (cap 5/hari
// membatasi abuse). AdMob SSV = hardening post-launch, dicatat sebagai risiko
// yang diterima. Jawaban: WalletUpdate atau Error("ad_cap_reached").
message ClaimAdTicket {}
// Jawaban: WalletUpdate (unlocked_item_key terisi) atau
// Error("insufficient_tickets" | "already_owned" | "premium_required").
message PurchaseItem { string item_key = 1; }
message WalletUpdate {
  int32 ticket_balance = 1;
  int32 ads_remaining_today = 2;
  string unlocked_item_key = 3;    // kosong kalau hanya perubahan saldo
}
// Client mengirim purchase token Play Billing setelah pembelian premium_unlock.
// MVP: server menyimpan token + set premium=TRUE; verifikasi penuh via Play
// Developer API menyusul saat service account Play Console siap (§8).
// Jawaban: ProfileData (premium=true) atau Error("purchase_invalid").
message PremiumPurchased { string play_purchase_token = 1; }

// --- Push token (prasyarat FCM your_turn / invitee_joined) ---
// Dikirim sekali per sesi setelah ServerHello (dan setiap kali token FCM
// di-refresh oleh Firebase SDK). Tanpa ini push tidak mungkin terkirim.
message RegisterPushToken { string fcm_token = 1; }
```

### 3.2 MySQL — tabel baru (extend baseline `players`/`matches`/`match_players`, tidak diubah strukturnya)

Konvensi: `CHAR(36)` UUID (sama seperti baseline), `player_id_lo`/`player_id_hi` = dua `players.id` diurutkan leksikografis untuk representasi pasangan tak berurut. **Catatan D8:** constraint "no player-count assumption" berlaku untuk data MATCH (`matches`/`match_players`, junction table, sudah N-player-safe, tidak disentuh) — `pair_ledger`/`pair_duo_score` pairwise-by-design sesuai GDD P1 ("Relasi antar pasangan pemain"), bukan pelanggaran D8.

```sql
-- Extend baseline
ALTER TABLE players
  ADD COLUMN fcm_token   VARCHAR(255) NULL,
  ADD COLUMN avatar_id   VARCHAR(32)  NOT NULL DEFAULT 'avatar_preset_01',
  ADD COLUMN frame_id    VARCHAR(32)  NOT NULL DEFAULT 'frame_default',
  ADD COLUMN premium     BOOLEAN      NOT NULL DEFAULT FALSE,
  ADD COLUMN premium_purchase_token VARCHAR(255) NULL,
  ADD COLUMN is_bot      BOOLEAN      NOT NULL DEFAULT FALSE,   -- diisi TRUE oleh UpsertBotPlayer (§4.8); dasar metrik %-vs-human (§4.9)
  ADD COLUMN bot_wins    INT          NOT NULL DEFAULT 0,       -- stat pribadi vs bot (GDD 5.1), bukan pair_ledger
  ADD COLUMN bot_losses  INT          NOT NULL DEFAULT 0,
  ADD COLUMN bot_draws   INT          NOT NULL DEFAULT 0;
-- end_reason: 'rematch_declined' TIDAK ada di enum — decline terjadi SETELAH
-- match selesai (itu event pairing, tercatat di rematch_decision.outcome).
ALTER TABLE matches
  ADD COLUMN end_reason ENUM('completed','forfeit_leave','forfeit_timeout') NULL,
  ADD COLUMN coop_score INT NULL;   -- skor bersama run co-op; NULL untuk versus
-- Hasil co-op bukan win/loss/draw — nilai baru 'coop_end' untuk kedua pemain.
ALTER TABLE match_players MODIFY COLUMN result ENUM('win','loss','draw','coop_end') NULL;

-- 1. PairLedger (rivalry, GDD §5.1)
CREATE TABLE IF NOT EXISTS pair_ledger (
    id               CHAR(36)     NOT NULL,
    player_id_lo     CHAR(36)     NOT NULL,
    player_id_hi     CHAR(36)     NOT NULL,
    game_id          VARCHAR(32)  NOT NULL,   -- '' = baris agregat all-games (GDD 5.1)
    wins_lo          INT          NOT NULL DEFAULT 0,
    wins_hi          INT          NOT NULL DEFAULT 0,
    draws            INT          NOT NULL DEFAULT 0,
    total_matches    INT          NOT NULL DEFAULT 0,
    streak_holder    CHAR(36)     NULL,
    streak_count     INT          NOT NULL DEFAULT 0,
    milestone_tier   ENUM('none','rivals','arch_rivals','nemesis') NOT NULL DEFAULT 'none',
    last_played_at   TIMESTAMP    NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_pair_ledger (player_id_lo, player_id_hi, game_id),
    KEY idx_pair_ledger_lo (player_id_lo),
    KEY idx_pair_ledger_hi (player_id_hi),
    CONSTRAINT fk_pl_lo FOREIGN KEY (player_id_lo) REFERENCES players(id),
    CONSTRAINT fk_pl_hi FOREIGN KEY (player_id_hi) REFERENCES players(id)
);
-- milestone_tier dihitung dari total_matches BARIS AGREGAT (game_id=''): 10=rivals, 50=arch_rivals, 100=nemesis.
-- Baris ditulis HANYA kalau kedua match_players.player_id di match tsb adalah is_bot=false (GDD 5.1: vs bot tidak masuk ledger).
-- PENTING (GDD 5.1: milestone dari versus+co-op DIGABUNG): match CO-OP juga
-- menaikkan total_matches baris agregat (game_id='') — tapi TIDAK menyentuh
-- wins/draws (itu khusus versus). Ditulis oleh RecordCoopResult (§4.8).

-- 2. PairDuoScore (co-op, GDD §5.2)
CREATE TABLE IF NOT EXISTS pair_duo_score (
    id                  CHAR(36)     NOT NULL,
    player_id_lo        CHAR(36)     NOT NULL,
    player_id_hi        CHAR(36)     NOT NULL,
    game_id             VARCHAR(32)  NOT NULL,  -- selalu game co-op nyata, tidak ada baris agregat
    best_score          INT          NOT NULL DEFAULT 0,
    total_coop_matches  INT          NOT NULL DEFAULT 0,
    last_played_at      TIMESTAMP    NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_pair_duo (player_id_lo, player_id_hi, game_id),
    CONSTRAINT fk_pds_lo FOREIGN KEY (player_id_lo) REFERENCES players(id),
    CONSTRAINT fk_pds_hi FOREIGN KEY (player_id_hi) REFERENCES players(id)
);
-- Duo level (Lv1-5, GDD 5.2: threshold 0/5/15/35/75) DIHITUNG saat query, bukan
-- disimpan redundan: SELECT SUM(total_coop_matches) FROM pair_duo_score WHERE
-- (player_id_lo=? AND player_id_hi=?).

-- 2b. Personal best co-op "with Bot" (GDD 5.2: skor co-op dengan bot TIDAK
--     masuk duo score pasangan; dicatat terpisah per pemain per game)
CREATE TABLE IF NOT EXISTS player_bot_best (
    player_id  CHAR(36)    NOT NULL,
    game_id    VARCHAR(32) NOT NULL,
    best_score INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, game_id),
    CONSTRAINT fk_pbb_player FOREIGN KEY (player_id) REFERENCES players(id)
);

-- 3. AsyncMatchState (item 5, hibernasi C4/BS — lihat §6.4)
CREATE TABLE IF NOT EXISTS async_match_state (
    match_id               CHAR(36)  NOT NULL,
    current_turn_player_id CHAR(36)  NULL,      -- NULL = match sudah selesai
    turn_started_at        TIMESTAMP NULL,
    nudge_push_sent_at     TIMESTAMP NULL,       -- dicegah kirim your_turn dobel oleh sweep
    forfeit_deadline       TIMESTAMP NULL,       -- turn_started_at + 48h
    board_state            BLOB      NOT NULL,   -- GameState.state bytes terakhir, untuk rehydrate tanpa replay
    updated_at             TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (match_id),
    CONSTRAINT fk_ams_match FOREIGN KEY (match_id) REFERENCES matches(id)
);
-- Hanya diisi untuk game_id IN ('connect_four','battleship') — 4 game real-time
-- tidak pernah async, tidak punya baris di sini.

-- 4. CosmeticItem + PlayerInventory (GDD §4)
CREATE TABLE IF NOT EXISTS cosmetic_item (
    id                CHAR(36)     NOT NULL,
    item_key          VARCHAR(64)  NOT NULL,  -- key stabil dari asset-list, mis. 'frame_01'
    type              ENUM('frame','emote_pack','board_skin','victory_anim') NOT NULL,
    price_tickets     INT          NOT NULL DEFAULT 0,
    premium_exclusive BOOLEAN      NOT NULL DEFAULT FALSE,
    PRIMARY KEY (id),
    UNIQUE KEY uq_cosmetic_key (item_key)
);
CREATE TABLE IF NOT EXISTS player_inventory (
    player_id   CHAR(36)  NOT NULL,
    item_id     CHAR(36)  NOT NULL,
    source      ENUM('ad_ticket','premium','default') NOT NULL,
    acquired_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, item_id),
    CONSTRAINT fk_pi_player FOREIGN KEY (player_id) REFERENCES players(id),
    CONSTRAINT fk_pi_item   FOREIGN KEY (item_id)   REFERENCES cosmetic_item(id)
);
-- Seed data: 19 baris dari asset-list §4 (item_key = SH-2x/SH-3x asset id, mis.
-- 'frame_01'..'frame_08' price 3, 'emote_pack_01'..'04' price 5, dst — lihat §4.6 seed script).

-- 5. TicketWallet (GDD §4)
CREATE TABLE IF NOT EXISTS ticket_wallet (
    player_id         CHAR(36) NOT NULL,
    balance           INT      NOT NULL DEFAULT 0,
    daily_ad_count    INT      NOT NULL DEFAULT 0,
    daily_count_date  DATE     NOT NULL,
    last_ad_at        TIMESTAMP NULL,
    PRIMARY KEY (player_id),
    CONSTRAINT fk_tw_player FOREIGN KEY (player_id) REFERENCES players(id)
);
-- Cap 5 ad/hari (GDD 4): app-layer reset daily_ad_count=0 saat daily_count_date
-- != CURDATE() sebelum increment, bukan cron terpisah.

-- 6. BotProfile (GDD §5.3)
CREATE TABLE IF NOT EXISTS bot_profile (
    id         CHAR(36)     NOT NULL,
    bot_key    VARCHAR(32)  NOT NULL,   -- momo|zippy|nova|rex|vega|apex
    name       VARCHAR(32)  NOT NULL,
    tier       ENUM('easy','medium','hard') NOT NULL,
    avatar_id  VARCHAR(32)  NOT NULL,   -- asset-list SH-27 key
    PRIMARY KEY (id),
    UNIQUE KEY uq_bot_key (bot_key)
);
CREATE TABLE IF NOT EXISTS bot_profile_game_availability (
    bot_profile_id CHAR(36)    NOT NULL,
    game_id        VARCHAR(32) NOT NULL,
    PRIMARY KEY (bot_profile_id, game_id),
    CONSTRAINT fk_bpga_bot FOREIGN KEY (bot_profile_id) REFERENCES bot_profile(id)
);
-- Seed: 6 bot x 6 game = 36 baris (semua bot tersedia semua game, GDD 5.3 eksplisit).

-- 7. Room (GDD §6.3)
CREATE TABLE IF NOT EXISTS room (
    id                      CHAR(36)    NOT NULL,
    room_code               VARCHAR(6)  NOT NULL,
    creator_player_id       CHAR(36)    NOT NULL,
    game_id_context         VARCHAR(32) NULL,   -- game yang inviter pilih saat share; NULL = share generik dari S3
    state                   ENUM('open','reserved','pending_host','paired','expired') NOT NULL DEFAULT 'open',
    reserved_for_player_id  CHAR(36)    NULL,   -- diisi saat invitee claim sementara host offline (PENDING_HOST)
    created_at              TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at              TIMESTAMP   NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_room_code_active (room_code),
    CONSTRAINT fk_room_creator  FOREIGN KEY (creator_player_id)      REFERENCES players(id),
    CONSTRAINT fk_room_reserved FOREIGN KEY (reserved_for_player_id) REFERENCES players(id)
);
-- uq_room_code_active aman selama row 'expired' rutin dibersihkan (sweep §6.3);
-- kalau tidak, code 6-char (33^6 ruang) akan habis dipakai state basi.

-- 8. RematchDecision (item 8, metrik gate — TAMBAHAN di luar daftar literal
--    user, dijustifikasi oleh kebutuhan "rematch rate per pairing")
CREATE TABLE IF NOT EXISTS rematch_decision (
    id         CHAR(36)    NOT NULL,
    match_id   CHAR(36)    NOT NULL,
    pair_key   VARCHAR(73) NOT NULL,  -- player_id_lo:player_id_hi
    outcome    ENUM('rematch_same_game','next_game','dissolved') NOT NULL,
    decided_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT fk_rd_match FOREIGN KEY (match_id) REFERENCES matches(id)
);
```

### 3.3 Client — data schema tambahan (C#, pure model, tidak MonoBehaviour)

```csharp
// Assets/Scripts/App/GameCatalog.cs — ScriptableObject, pola sama seperti ServerConfig
[CreateAssetMenu(fileName = "GameCatalog", menuName = "2UP/Game Catalog")]
public class GameCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string gameId;          // harus match ID kanonik di §2 persis
        public string sceneName;       // scene S6 game ini, mis. "AirHockey" — dipakai AppStateMachine.ToGame(gameId)
        public string displayName;
        public GameMode mode;          // Versus | CoOp
        public GamePacing pacing;      // Live | TurnBased
        public Sprite cardArt;         // asset-list gamecard_xx
    }
    public enum GameMode { Versus, CoOp }
    public enum GamePacing { Live, TurnBased }
    public Entry[] entries;            // 6 entri, urutan = urutan produksi GDD §7
    public Entry Find(string gameId) => System.Array.Find(entries, e => e.gameId == gameId);
}

// Assets/Scripts/App/MatchContext.cs — TAMBAHAN field (file existing, extend saja)
public static partial class MatchContext
{
    public static string PairId;                 // dari PairFound, sebelum game_id diketahui
    public static string PendingRoomCode;        // dari deep link / install referrer (§6.3b); dikonsumsi sekali setelah ServerHello
}

// Assets/Scripts/App/AppStateMachine.cs — EXTEND (pola existing dipertahankan:
// scene load HANYA lewat sini). State enum bertambah; Lobby dihapus.
public class AppStateMachine : MonoBehaviour
{
    public enum State { Boot, Home, Invite, Queue, Voting, InGame, Result, Profile, Shop, Settings, AsyncList }
    [SerializeField] private GameCatalog catalog;   // wired oleh SkeletonBuilder di App object

    public void ToBoot();
    public void ToHome();          // menggantikan ToLobby()
    public void ToInvite();
    public void ToQueue();
    public void ToVoting();
    public void ToGame(string gameId);   // SceneManager.LoadScene(catalog.Find(gameId).sceneName) — hardcode "ConnectFour" dihapus
    public void ToResult();
    public void ToProfile();
    public void ToShop();
    public void ToSettings();
    public void ToAsyncList();
    public void SetResult();       // existing, tetap (sub-state dalam scene game sebelum ToResult)
}
```

---

## 4. Services & API contracts

### 4.1 `internal/game` — dua interface opsional baru (base `Game`/`Bot` TIDAK berubah)

```go
// internal/game/game.go — TAMBAHAN, base Game/Bot interface di atasnya persis sama seperti sekarang.

// PerPlayerGame ditambahkan oleh game yang state-nya berbeda per pemain (fog
// of war). Runner mengecek lewat type assertion; game yang tidak
// mengimplementasikan ini (5 dari 6 game) tetap pakai State() seperti sekarang.
type PerPlayerGame interface {
    Game
    StateFor(playerID string) *pb.GameState
}

// Resumable ditambahkan oleh game yang mendukung hibernasi + resume dari
// state tersimpan (async turn-based: connect_four, battleship). Game
// real-time tidak mengimplementasikan ini.
type Resumable interface {
    Game
    LoadState(players []PlayerRef, state []byte) error
}

// TurnBased ditambahkan oleh game bergiliran yang butuh turn timer mode live
// (AC-C4-06). Runner memakai ini untuk men-skip giliran saat timer habis —
// game tetap tidak tahu-menahu soal waktu (Runner yang memegang timer).
type TurnBased interface {
    Game
    NextPlayerID() string // pemain yang wajib bergerak sekarang; "" kalau tidak ada (mis. BS_PLACEMENT)
    SkipTurn()            // giliran hangus TANPA move (AC-C4-06: disc tidak dijatuhkan)
}

// BotTier dipindah ke package game (dipakai semua game + StartBotMatch §3.1.2).
type BotTier int
const (
    TierEasy BotTier = iota
    TierMedium
    TierHard
)

// Factory.NewBot berubah signature (satu-satunya perubahan pada tipe existing;
// connectfour/init() ikut disesuaikan): bot sekarang dibuat per tier.
type Factory struct {
    New    func() Game
    NewBot func(self PlayerRef, tier BotTier) Bot
}
```

`connectfour` package: logika inti `Game`/`Bot` TIDAK berubah. **Tambahan** (additive terhadap perilaku existing): (a) `Resumable.LoadState` (deserialize `ConnectFourState` dari bytes ke `g.cells`/`g.next`/`g.moves`/`g.finished`) supaya C4 lolos hibernasi async (§6.4); (b) `TurnBased` (`NextPlayerID()` sudah tersedia dari `g.players[g.next]`; `SkipTurn()` = `g.next = (g.next+1) % len(g.players)` tanpa menaruh disc — AC-C4-06); (c) `init()` disesuaikan ke signature `NewBot(self, tier)` baru, heuristik existing = `TierMedium` (§4.6). `Battleship` mengimplementasikan `PerPlayerGame`, `Resumable`, dan `TurnBased` (`NextPlayerID()` mengembalikan `""` selama `BS_PLACEMENT`). 4 game real-time (`reflex_duel`, `air_hockey`, `wall_defense`, `keepup_duo`) mengimplementasikan HANYA base `Game`/`Bot` — persis pola `connectfour` sekarang.

### 4.2 `internal/match` — `Service`, `Pairing`, `Runner` (extend, pola actor tidak berubah)

```go
// Service — GameID dihapus dari struct (tidak lagi 1 Service = 1 game).
type Service struct {
    Store         store.Store
    Log           *slog.Logger
    RematchWindow time.Duration
    BotMoveDelay  time.Duration
}

// StartMatch sekarang menerima gameID eksplisit dan callback onExit — dipanggil
// Pairing setelah voting LOCKED, bukan langsung dari Matchmaker/Rooms lagi.
// onExit(nextGameRequested=true) berarti Runner berhenti karena kedua pemain
// (atau salah satu) pilih "Next Game" (RematchChoice.NEXT_GAME) — Pairing
// harus membuka voting baru untuk pasangan yang SAMA. onExit(false) berarti
// forfeit/leave/rematch_declined — Pairing membubarkan diri (GDD 6.2.4 dissolved).
func (s *Service) StartMatch(gameID string, parts []Participant, onExit func(nextGameRequested bool)) error

// HandleInput/HandleLeave: signature TIDAK BERUBAH.
func (r *Runner) HandleInput(playerID string, input *pb.GameInput)
func (r *Runner) HandleLeave(playerID string)
// HandleRematch: signature bertambah choice, accept lama tetap ada (accept=false = decline/leave, tidak berubah makna).
func (r *Runner) HandleRematch(playerID, matchID string, accept bool, choice pb.RematchChoice)
// HandleReattach — BARU (reconnect §4.4): menukar Participant lama (session
// mati) dengan session baru untuk playerID yang sama. Diproses sebagai cmd di
// run goroutine (parts hanya disentuh goroutine run — tidak ada akses lintas
// goroutine): cari index infos[i].player_id == playerID, ganti parts[i],
// lalu kirim GameState terkini HANYA ke participant baru itu.
func (r *Runner) HandleReattach(playerID string, p Participant)

// internal/match/gamerules.go — BARU: konfigurasi per-game yang dibaca Runner
// dan session (grace §4.4, tick §4.7, turn timer AC-C4-06). Satu tempat, bukan
// tersebar di if-else per game.
type GraceOutcome int
const (
    GraceForfeit      GraceOutcome = iota // sisa pemain menang forfeit
    GraceConvertAsync                     // hibernasi ke async_match_state (§6.4)
    GraceEndRun                           // co-op: run berakhir, skor tetap dicatat
)
type GameRules struct {
    TickInterval time.Duration // 0 = event/turn-based (tanpa loop tick)
    TurnTimer    time.Duration // 0 = tanpa turn timer per giliran (mode live)
    MaxTurnSkips int           // giliran hangus beruntun pemain sama -> forfeit
    LiveGrace    time.Duration // §4.4
    Grace        GraceOutcome
    AsyncCapable bool
    CoOp         bool
}
var RulesByGame = map[string]GameRules{
    "connect_four": {TurnTimer: 30 * time.Second, MaxTurnSkips: 3, LiveGrace: 30 * time.Second, Grace: GraceConvertAsync, AsyncCapable: true},
    "battleship":   {LiveGrace: 30 * time.Second, Grace: GraceConvertAsync, AsyncCapable: true}, // tanpa TurnTimer: giliran live BS tetap pakai deadline 24h (AC-BS-05)
    "reflex_duel":  {LiveGrace: 10 * time.Second, Grace: GraceForfeit},
    "air_hockey":   {TickInterval: 50 * time.Millisecond, LiveGrace: 20 * time.Second, Grace: GraceForfeit},
    "wall_defense": {TickInterval: 50 * time.Millisecond, LiveGrace: 15 * time.Second, Grace: GraceEndRun, CoOp: true},
    "keepup_duo":   {TickInterval: 50 * time.Millisecond, LiveGrace: 0, Grace: GraceEndRun, CoOp: true},
}

// Turn timer (AC-C4-06), di Runner.run(): kalau RulesByGame[gameID].TurnTimer > 0
// DAN game mengimplementasikan TurnBased DAN mode match = live (bukan hasil
// resume async): arm timer tiap giliran baru (reset setelah tiap ApplyInput
// sukses). Timer habis -> catat skip untuk NextPlayerID(), game.SkipTurn(),
// broadcast GameState baru; skip beruntun pemain sama mencapai MaxTurnSkips ->
// forfeitResult(pemain itu) (AC-C4-06: 3 giliran hangus = forfeit). Mode async
// (§6.4) MENGABAIKAN TurnTimer (AC-C4-07: tanpa turn timer per giliran).

// Runner.run(): resolusi rematch berubah dari accepts map[string]bool jadi
// decisions map[string]pb.RematchChoice. Kedua pemain harus submit (accept=true)
// sebelum dievaluasi:
//   - semua decisions == REMATCH_SAME_GAME  -> r.startGame() (loop internal SAMA seperti sekarang, tidak berubah)
//   - ADA SATU decisions == NEXT_GAME        -> Runner keluar, panggil onExit(true)
//   - ada accept=false dari siapa pun         -> broadcastError("rematch_declined", ...), onExit(false) (perilaku v0 tidak berubah)
// Setiap kali decision baru masuk: broadcast RematchStatusUpdate{match_id, player_id, choice} (GDD 6.2.3 real-time status).
// CATATAN CONFIG: Config.RematchWindow default berubah 30s -> 20s di
// fillDefaults() (GDD 6.2: DECIDING window = 20 detik).

// Pairing — actor BARU, pola identik Runner (chan cmd, goroutine tunggal,
// broadcast/sendTo helper). Menjalankan PAIRED -> VOTING -> LOCKED|SHOWDOWN ->
// COUNTDOWN. Setelah COUNTDOWN, memanggil Service.StartMatch dan menunggu
// onExit; kalau onExit(true) (Next Game), Pairing membuka ronde voting baru
// untuk parts YANG SAMA tanpa re-queue. Kalau onExit(false) atau salah satu
// Participant leave saat voting, Pairing selesai (parts dilepas, sesi kembali idle).
type Pairing struct {
    svc     *Service
    catalog *GameCatalog        // map[gameID]bool, daftar 6 game terdaftar; lihat §4.5
    parts   []Participant
    pairID  string
    cmds    chan pairingCmd
    done    chan struct{}
    log     *slog.Logger
    // state VOTING/SHOWDOWN, votes map[string]string, dst — detail implementasi
}

func NewPairing(svc *Service, catalog *GameCatalog, parts []Participant) *Pairing
func (p *Pairing) HandleVote(playerID, gameID string)          // dari VoteGame
func (p *Pairing) HandleShowdownPick(playerID, gameID string)  // dari ShowdownPick
func (p *Pairing) HandleLeave(playerID string)                 // sama pola Runner.HandleLeave
```

**Algoritma voting (`Pairing.run()`), persis GDD §6.1:**
1. Masuk `VOTING`, broadcast `PairFound` lalu mulai timer 15000ms.
2. Vote masuk (`HandleVote`) → simpan, broadcast `VoteUpdate` (seluruh map `votes_by_player_id` saat ini).
3. Begitu SEMUA participant sudah vote:
   - Sama → `LOCKED`: broadcast `VotingLocked{game_id, countdown_ms:3000}`, tunggu 3000ms server-side, lalu `Service.StartMatch(game_id, parts, onExit)`.
   - Beda → `SHOWDOWN`: broadcast `VotingShowdown{candidate_game_ids:[a,b], deadline:+10000ms}`. `ShowdownPick` dari salah satu → treat sebagai vote baru = kartu yang dipilih, keduanya jadi sama → `LOCKED`. Timeout 10s tanpa pick → server pilih random di antara 2 kandidat (uniform) → `LOCKED`.
4. Timer 15000ms habis dengan hanya SATU yang vote → pilihan yang vote otomatis `LOCKED` (GDD 6.1.4).
5. Timer 15000ms habis dengan NOL vote → server pilih random dari 6 game dengan bobot: `reflex_duel` weight 3, 5 game lain weight 1 masing-masing (GDD 6.1.5: "bobot ke game tercepat") → `LOCKED`.
6. Participant leave saat `VOTING`/`SHOWDOWN` → broadcast `VotingCancelled{reason:"opponent_left"}` ke sisi yang tinggal, Pairing selesai.

### 4.3 `internal/match/matchmaker.go`, `rooms.go` — perubahan titik integrasi

```go
// Matchmaker.Join dan Rooms.Join TIDAK lagi memanggil svc.StartMatch langsung.
// Keduanya memanggil satu titik bersama:
func (svc *Service) pair(parts []Participant) {
    p := NewPairing(svc, defaultCatalog, parts)
    for _, part := range parts { part.Attach(p, ...) } // Attach sekarang generik: bisa attach ke Pairing atau Runner
    go p.run()
}
```
`Participant.Attach` perlu digeneralisasi dari `Attach(r *Runner, self *pb.PlayerInfo)` menjadi menerima tipe actor manapun yang punya `HandleInput`/`HandleLeave` — **[DECIDED]**: definisikan interface kecil `Actor` yang diimplementasikan baik `*Runner` maupun `*Pairing`:
```go
type Actor interface {
    HandleLeave(playerID string)
}
func (s *session) Attach(a match.Actor, self *pb.PlayerInfo)  // session.go, generalisasi dari Attach(r *Runner, ...)
```
Session menyimpan `actor match.Actor` alih-alih `runner *match.Runner`; saat menerima `VoteGame`/`GameInput`/dst., type-assert ke `*Pairing` atau `*Runner` sesuai kebutuhan pesan (pola sama seperti `session.handle()` existing yang sudah switch berdasarkan `Envelope.PayloadCase`).

### 4.4 Reconnect (item 7)

```go
// internal/server/server.go — TAMBAHAN
type Server struct {
    // ... field existing ...
    parkedMu sync.Mutex
    parked   map[string]parkedSeat  // player_id -> actor + grace deadline, diisi saat disconnect di tengah match
}
type parkedSeat struct {
    actor        match.Actor
    graceUntil   time.Time
    graceSeconds int
}
```

Alur (session.go, `close()` dan `handleHello()`):
1. `session.close()`: kalau `s.actor != nil` (sedang dalam Pairing atau Runner) DAN game sedang `phasePlaying`, JANGAN langsung `actor.HandleLeave(playerID)`. Sebaliknya: daftarkan `parkedSeat{actor, graceUntil: now+graceSeconds}` di `Server.parked`, mulai `time.AfterFunc(graceSeconds, func(){ jika masih parked -> actor.HandleLeave(playerID) })`. Voting (`Pairing`) TIDAK punya grace period — leave saat voting selalu langsung `HandleLeave` (GDD 6.1.6 tidak menyebut grace untuk voting).
2. `session.handleHello()`: setelah `UpsertPlayer` sukses, cek `Server.parked[playerID]`. Ada dan belum kadaluarsa → batalkan timer forfeit, re-`Attach` session ini ke `actor` yang sama, kirim `MatchResumed{match_id, game_id, state, grace_seconds_remaining}` (state didapat dari `actor` kalau masih Runner aktif; untuk match yang sudah dikonversi ke async dan Runner-nya sudah hibernasi, lihat §6.4 — resume lewat `ResumeAsyncMatch`, bukan lewat grace path ini).
3. Grace period per game (dari GDD §7 AC per game, TIDAK didefault — semua sudah eksplisit di GDD):

| Game | Grace | Habis grace → |
|------|-------|----------------|
| Connect Four | 30s | konversi ke async (Runner TIDAK forfeit, lanjut §6.4 hibernasi) — AC-C4-08 |
| Battleship | 30s **[DECIDED: samakan dengan C4]** | konversi ke async — AC-BS-05 tidak beri angka eksplisit tapi menyatakan pola live/async sama seperti C4 |
| Reflex Duel | 10s | forfeit — AC-RD-07 |
| Air Hockey | 20s | forfeit — AC-AH-05 |
| Wall Defense | 15s | run berakhir, skor saat itu tetap dicatat (`co_op_score`, bukan forfeit win/loss) — AC-WD-05 |
| Keep-Up Duo | 0s (instan) | run berakhir seketika, skor dicatat — AC-KU-05 |

### 4.5 `GameCatalog` (server, static registry-of-registries — bukan tabel DB, sinkron manual dengan `game.Register` calls)

```go
// internal/match/catalog.go
type GameCatalog struct {
    IDs []string // urutan tetap: connect_four, reflex_duel, air_hockey, wall_defense, keepup_duo, battleship
}
func (c *GameCatalog) Contains(gameID string) bool
var defaultCatalog = &GameCatalog{IDs: []string{
    "connect_four", "reflex_duel", "air_hockey", "wall_defense", "keepup_duo", "battleship",
}}
```
`server.New()`: hapus `GameID: "connect_four"` hardcode dari `match.Service{}`, tambahkan 5 import blank baru (`_ "internal/game/reflexduel"`, dst.) persis pola `_ "internal/game/connectfour"` yang sudah ada.

### 4.6 Bot parameter per tier per game (item 4 — angka eksplisit)

Tipe `game.BotTier` (§4.1) dipakai semua game; `Factory.NewBot(self, tier)`. Bot dari queue timeout = `TierMedium`, roster random Nova/Rex (GDD 5.3); bot dari `StartBotMatch` = tier pilihan pemain, roster random dalam tier dari `bot_profile`.

```go
// internal/game/connectfour/bot.go — EXTEND, heuristik existing (block-then-win-
// then-random) menjadi definisi TierMedium persis apa adanya sekarang.
const (
    EasyIgnoreBlockChance = 0.35 // TierEasy = Medium tapi 35% peluang ABAIKAN blok yang tersedia
    HardLookaheadDepth    = 3    // TierHard = Medium + minimax depth 3; eval: menang langsung=+1000, hitung open-3 milik sendiri minus lawan
)
// Move delay SEMUA tier: rand[1000ms, 3000ms] (AC-C4-10, sudah eksplisit di GDD, tidak berubah).
```

```go
// internal/game/reflexduel/bot.go
var ReactionMsRange = map[BotTier][2]int{
    TierEasy:   {450, 800},
    TierMedium: {280, 450},
    TierHard:   {190, 280},
}
var FalseStartChance = map[BotTier]float64{
    TierEasy:   0.10,  // GDD eksplisit
    TierMedium: 0.03,  // [DECIDED]
    TierHard:   0.005, // [DECIDED]
}
```

```go
// internal/game/airhockey/bot.go
const MalletSpeedCapUnitsPerSec = 900.0  // [DECIDED][TUNING] SATU nilai, dipakai untuk cap bot DAN clamp input manusia (AC-AH-06: "fair by construction") — lihat §4.7. 900 u/s = 1,2 detik menyeberangi meja; hampir pasti direvisi playtest pertama, sengaja satu konstanta supaya murah diubah
var DecisionLatencyMs = map[BotTier]int{TierEasy: 250, TierMedium: 120, TierHard: 60}
// Easy: hanya bereaksi kalau puck.y dalam 300 unit dari garis gawang sendiri (reaktif murni, AC-AH-06).
// Medium: intersep — prediksi posisi puck saat puck.x mencapai mallet.x pakai puck_vx/puck_vy saat ini.
// Hard: intersep + arahkan pantulan ke titik 80% menuju sudut gawang terjauh dari posisi mallet lawan saat ini.
```

```go
// internal/game/walldefense/bot.go
var ReactionLatencyMs = map[BotTier]int{TierEasy: 300, TierMedium: 150, TierHard: 100}
// Easy: hanya cover x-range separuh sisi sendiri (AC-WD-06).
// Medium: cover penuh sisi sendiri.
// Hard: cover sisi sendiri + assist hingga 30% masuk sisi partner kalau target_x partner
//       > 150 unit dari paddle partner DAN paddle partner belum bergerak 400ms terakhir.
```

```go
// internal/game/keepupduo/bot.go
var MissChance = map[BotTier]float64{TierEasy: 0.20, TierMedium: 0.05, TierHard: 0.01} // GDD eksplisit Easy/Medium, Hard [DECIDED]
// Hard: arahkan pantulan ke x=0.5 (tengah area) — "memposisikan pantulan enak untuk manusia" (GDD 5.3/AC-KU-06).
```

```go
// internal/game/battleship/bot.go
// Easy: tembak sel belum-ditembak acak murni.
// Medium: hunt/target — random sampai Hit, lalu coba 4 tetangga (urutan diacak) sampai Sunk, balik ke random.
// Hard: hunt/target + PARITY SEARCH saat fase hunt: hanya target sel (row+col)%2==0
//       (valid karena kapal terkecil panjang 2, setiap kapal PASTI menempati minimal satu sel parity-0).
// Move delay semua tier: rand[2000ms, 5000ms] (AC-BS-07, eksplisit GDD).
```

### 4.7 Physics contract — real-time games (item 1, server-authoritative)

- **Tick rate:** 20 Hz (50ms) untuk `air_hockey`, `wall_defense`, `keepup_duo` — semua sharing pola simulasi step tetap + broadcast full-snapshot `GameState` tiap tick (konsisten dengan komentar existing di `session.go`: "GameState is always a full snapshot"). `reflex_duel` TIDAK tick-based (event-driven: ARMED/tap), tidak butuh tick rate.
- **Koordinat:** unit logis 0-1080 (lebar) — sama dengan `CanvasScaler` reference client, lihat §2.
- **Interpolation tolerance (client):** render posisi via interpolasi linear antara 2 snapshot terakhir sepanjang interval tick (50ms); kalau snapshot berikutnya telat >100ms (2 tick), berhenti ekstrapolasi dan snap ke posisi terakhir (cegah rubber-banding terlihat).
- **Input rate:** client mengirim `AirHockeyInput`/`WallDefenseInput`/`KeepUpDuoInput` pada delta drag, DIBATASI maksimum 20 Hz di sisi client (match tick rate) — **[DECIDED]** `NetworkClient` mendapat method baru `SendRateLimited(Envelope, string key, float minIntervalSeconds)` supaya scene real-time tidak membanjiri socket melebihi tick server.
- **Speed cap (fairness, AC-AH-06):** server clamp SEMUA `AirHockeyInput.mallet_*` delta ke `MalletSpeedCapUnitsPerSec` (§4.6) sebelum apply — berlaku sama untuk human dan bot, mustahil manusia mengalahkan cap dengan drag cepat (anti speedhack sekaligus definisi fairness GDD).
- **Orientasi render Wall Defense (AC-WD-01, "detail kamera di TDD"):** arena disimulasikan dalam SATU ruang koordinat bersama (lebar 1080; gawang bersama di tepi bawah y=0; bola datang dari atas). **Kedua client me-render orientasi identik** (gawang di bawah) — TIDAK ada mirroring per seat, supaya koordinasi verbal ("tutup sisi kananmu!") konsisten antar device. Identitas pemain dibedakan warna paddle (`paddle_p1`/`paddle_p2`, WD-02) + label "YOU" kecil di atas paddle sendiri. Paddle bebas bergerak sepanjang X penuh [0,1080]; posisi spawn seat 0 x=270, seat 1 x=810 (definisi "area sendiri" untuk bot §4.6: seat 0 = x∈[0,540], seat 1 = x∈[540,1080]).
- **Wave curve Wall Defense (AC-WD-03):** `ball_count(N) = min(2 + floor(N/2), 8)`; `ball_speed(N) = 180 * (1 + 0.12*(N-1))` unit/s, cap di `450` (≈2.5x) setelah wave 13. Wave bersih → broadcast `wave_transition=true` 1500ms → wave berikutnya spawn.
- **Speed curve Keep-Up Duo (AC-KU-03):** `ball_speed(score) = 220 * (1 + 0.03*score)` unit/s, cap `440` (2x) di score ≥33.
- **Reflex Duel latency compensation (AC-RD-03):** waktu reaksi dihitung `server_receipt_time - go_server_ts_ms - (rtt_estimate/2)`, di mana `rtt_estimate` = sample RTT terbaru dari `Ping`/`Pong` sesi tsb (maks umur 3 detik; lebih tua dari itu → `rtt_estimate=0`, hasil ditandai tidak terkompensasi tapi tetap dipakai, tidak block gameplay). **[DECIDED]** client mem-ping tiap 2 detik (bukan 15 detik default `NetworkClient`) selama match `reflex_duel` aktif, supaya sample RTT cukup segar — lihat §9 catatan client.

---

### 4.8 `store.Store` — extension interface (semua signature; MySQL DAN memory)

4 method existing (`UpsertPlayer`/`CreateMatch`/`FinishMatch`/`Close`) tidak berubah. Semua method baru diimplementasikan di `mysql.go` **dan** `memory.go` — logika ledger/wallet/room hidup di store, sehingga integration test tetap jalan tanpa DB (konvensi repo: `make test` no-DB). Konstanta baru: `ResultCoop = "coop_end"`.

```go
// --- Tipe hasil (dipakai lintas server + dipetakan ke proto §3.1.7) ---
type PlayerMeta struct {
    PlayerID, DisplayName, AvatarID, FrameID string
    Premium                                  bool
    BotWins, BotLosses, BotDraws             int
}
type LedgerDelta struct {
    WinsLo, WinsHi, Draws, TotalMatchesAgg int
    StreakHolderID                         string
    StreakCount                            int
    MilestoneTier                          string // none|rivals|arch_rivals|nemesis
    MilestoneJustReached                   bool
}
type DuoDelta struct {
    BestScore, TotalCoopMatches, DuoLevel int
    NewBest                               bool
}
type PairAggregate struct { // baris agregat + duo level, bahan PairSummary
    OtherPlayerID, OtherDisplayName, OtherAvatarID string
    WinsMine, WinsTheirs, Draws, TotalMatches      int
    MilestoneTier                                  string
    DuoLevel                                       int
    LastPlayedAt                                   time.Time
}
type PairDetailData struct {
    Aggregate    PairAggregate
    VersusLines  []PairGameLine // {GameID string; WinsMine, WinsTheirs, Draws int}
    CoopLines    []DuoGameLine  // {GameID string; BestScore, TotalMatches int}
    StreakHolder string
    StreakCount  int
}
type AsyncMatchRow struct {
    MatchID, GameID, TurnPlayerID string // TurnPlayerID "" = fase placement (BS)
    TurnStartedAt, ForfeitDeadline time.Time // zero = NULL (placement tanpa deadline, AC-BS-02)
    State                          []byte
    Players                        []MatchPlayer
}
type RoomRow struct {
    Code, CreatorID, GameContext, State, ReservedFor string
    ExpiresAt                                        time.Time
}
type CosmeticRow struct {
    ItemKey, Type    string
    PriceTickets     int
    PremiumExclusive, Owned bool
}

type Store interface {
    // ... 4 method existing, tidak berubah ...

    // Meta / profile
    GetPlayerMeta(ctx context.Context, playerID string) (PlayerMeta, error)
    SetPlayerProfile(ctx context.Context, playerID, displayName, avatarID, frameID string) error // "" = field tidak diubah; equip divalidasi terhadap player_inventory
    SetPushToken(ctx context.Context, playerID, fcmToken string) error
    SetPremium(ctx context.Context, playerID, playPurchaseToken string) error
    UpsertBotPlayer(ctx context.Context, botKey, displayName string) (playerID string, err error) // is_bot=TRUE; menggantikan pola "bot:"+uuid via UpsertPlayer di matchmaker.newBot

    // Rivalry ledger & duo (dipanggil Runner.persistFinish; no-op+zero kalau ada bot di pasangan)
    RecordVersusResult(ctx context.Context, gameID, playerA, playerB, winnerID string, draw bool) (LedgerDelta, error)
    RecordCoopResult(ctx context.Context, gameID, playerA, playerB string, score int) (DuoDelta, error) // juga ++total_matches baris agregat pair_ledger (milestone gabungan, §3.2)
    RecordBotResult(ctx context.Context, playerID, gameID, result string, coopScore int) error          // bot_wins/losses/draws + upsert player_bot_best
    ListPairs(ctx context.Context, playerID string) ([]PairAggregate, error)
    GetPairDetail(ctx context.Context, playerID, otherPlayerID string) (PairDetailData, error)

    // Async (§6.4)
    SaveAsyncState(ctx context.Context, matchID, turnPlayerID string, deadline time.Time, state []byte) error
    LoadAsyncMatch(ctx context.Context, matchID string) (AsyncMatchRow, error)
    ListAsyncMatchesFor(ctx context.Context, playerID string) ([]AsyncMatchRow, error)
    DeleteAsyncState(ctx context.Context, matchID string) error
    DueAsyncNudges(ctx context.Context, turnOlderThan time.Time) ([]AsyncMatchRow, error) // skip baris deadline NULL (placement)
    MarkNudgeSent(ctx context.Context, matchID string) error
    DueAsyncForfeits(ctx context.Context, now time.Time) ([]AsyncMatchRow, error)

    // Rooms (§6.3 — DB adalah source of truth TTL/klaim; map in-memory Rooms existing
    // tinggal cache Participant host yang sedang online, keyed by code)
    CreateRoom(ctx context.Context, code, creatorID, gameContext string, expiresAt time.Time) error
    GetRoom(ctx context.Context, code string) (RoomRow, error)
    UpdateRoomState(ctx context.Context, code, state, reservedForPlayerID string, expiresAt time.Time) error
    ExpireRoomsBefore(ctx context.Context, now time.Time) ([]RoomRow, error) // set 'expired', kembalikan yang berubah (untuk notifikasi RoomExpired)
    PurgeExpiredRoomsBefore(ctx context.Context, cutoff time.Time) error     // hapus row 'expired' > 24h

    // Wallet & shop (S9)
    ListCosmetics(ctx context.Context, playerID string) ([]CosmeticRow, error)
    GetWallet(ctx context.Context, playerID string) (balance, adsRemainingToday int, err error)
    AddAdTicket(ctx context.Context, playerID string, dailyCap int) (newBalance, adsRemaining int, err error) // atomik: reset harian kalau daily_count_date != CURDATE(); ErrDailyCapReached
    PurchaseCosmetic(ctx context.Context, playerID, itemKey string) (newBalance int, err error)               // tx: saldo cukup + belum dimiliki + cek premium_exclusive
    GrantCosmetic(ctx context.Context, playerID, itemKey, source string) error                                // premium claim / default seed

    // Metrik (§4.9)
    SetMatchEndReason(ctx context.Context, matchID, reason string) error
    SetMatchCoopScore(ctx context.Context, matchID string, score int) error
    RecordRematchDecision(ctx context.Context, matchID, pairKey, outcome string) error
}
```

Seed script (`cmd/twoup-seed` atau statement idempotent di `schema[]`): 19 baris `cosmetic_item` dari GDD §4 (8 frame @3, 4 emote_pack @5, 2 board_skin C4 @4, 2 board_skin AH @4, 1 board_skin BS @4, 2 victory_anim @6; `frame_golden_crown` premium_exclusive=TRUE price 0), 6 baris `bot_profile` + 36 baris availability.

### 4.9 Instrumentasi metrik gate (item 8)

Tiga metrik GDD §8, sumber data §3.2, **titik tulis** eksplisit:

| Kolom/tabel | Ditulis oleh | Kapan |
|---|---|---|
| `matches.end_reason='completed'` | `Runner.persistFinish` | game selesai normal (menang/draw/co-op run end) |
| `matches.end_reason='forfeit_leave'` | `Runner` jalur `cmdLeave`/grace-forfeit/3-skip | pemain pergi atau grace habis (game non-async) |
| `matches.end_reason='forfeit_timeout'` | async sweep (§6.4) | deadline 48h lewat |
| `matches.coop_score` | `Runner.persistFinish` (game co-op) | bersamaan end_reason |
| `rematch_decision` | `Runner` saat DECIDING resolve | outcome: `rematch_same_game` / `next_game` / `dissolved` (leave/decline/timeout 20s) |
| `players.is_bot` | `UpsertBotPlayer` | pembuatan bot |

Query gate (dijalankan manual/dashboard, bukan kode produk):

```sql
-- 1. Match completion rate (harian)
SELECT DATE(finished_at) d, ROUND(SUM(end_reason='completed')/COUNT(*)*100, 1) AS pct
FROM matches WHERE status='finished' GROUP BY d;

-- 2. Persen match vs human (match tanpa satu pun bot)
SELECT ROUND(SUM(has_bot=0)/COUNT(*)*100, 1) AS pct_vs_human FROM (
  SELECT m.id, MAX(p.is_bot) AS has_bot
  FROM matches m JOIN match_players mp ON mp.match_id=m.id JOIN players p ON p.id=mp.player_id
  WHERE m.status='finished' GROUP BY m.id
) t;

-- 3. Rematch rate per pairing (lanjut main lagi / semua keputusan)
SELECT pair_key,
       ROUND(SUM(outcome IN ('rematch_same_game','next_game'))/COUNT(*)*100, 1) AS continue_pct
FROM rematch_decision GROUP BY pair_key;
```

## 5. Scene & screen map

Semua scene baru mengikuti pola `Assets/Editor/SkeletonBuilder.cs` (editor-authored, UGUI+TMP, `CanvasScaler` 1080×1920, `GridLayoutGroup`/`HorizontalLayoutGroup` sesuai kebutuhan, `Place()`/`CreateButton()`/`CreateText()`/`SetRef()` helper existing di-extend, bukan ditulis ulang). Root tiap screen tetap `Screen_<Nama>` di bawah satu `UICanvas` per scene aktif (konvensi client sudah ada). Referensi sprite = ID persis dari `asset-list.md`.

| # | Scene file | Controller | GameObject utama | Asset (asset-list ID) |
|---|-----------|------------|-------------------|------------------------|
| S1/S2 | `Boot.unity` (existing, extend), `Home.unity` (baru — **menggantikan `Lobby.unity` skeleton**: scene + `BuildLobbyScene()` dihapus dari SkeletonBuilder, `LobbyController.cs` dipensiunkan; fungsinya terbagi ke Home/InviteRoom/Queue) | `BootController` (existing), `HomeController` (baru) | Home: `Btn_PlayWithFriend`, `Btn_QuickMatch`, `Btn_VsBot`, `Badge_AsyncCount`, `Btn_Profile`/`Btn_Shop`/`Btn_Settings` | SH-16 bg_home, SH-13 icon_set_ui |
| S3 | `InviteRoom.unity` (baru) | `InviteRoomController` | `Panel_CreateRoom` (kode 6-char besar + `Btn_ShareDeepLink`), `Panel_JoinRoom` (`InputField_RoomCode` pakai `input_field_code`), `Text_WaitingOpponent`, `Text_TtlCountdown` | SH-14 input_field_code |
| S4 | `Queue.unity` (baru) | `QueueController` | `Text_QueueStatus`, `Anim_MatchFound` (Image, main dari `anim_matchfound` frame sheet), `Btn_Cancel` | SH-35 anim_matchfound |
| S5 | `Voting.unity` (baru) | `VotingController` | `Grid_GameCards` (`GridLayoutGroup`, 6x `GameCard` prefab-equivalent = child dibuat per `GameCatalog.entries`), tiap card: `Image_Art` (gamecard_xx) + `Tag_Mode`/`Tag_Pacing` + highlight state; `Panel_Showdown` (2 card + `Anim_CoinFlip`); `Text_PairBadge` (milestone/duo level, data dari `PairFound` §3.1.2); **`Panel_BotPicker`** (tampil saat entry via `Btn_VsBot` dari S2, menggantikan flow voting: grid game sama + segmen tier Easy/Medium/Hard + `Btn_StartBotMatch` → kirim `StartBotMatch{game_id, tier}`) | SH-30 gamecard_xx (x6), SH-31 tag_* (x4), SH-34 anim_coinflip, SH-32/33 badge_* |
| S6 | `<GameId>.unity` x6 (`ConnectFour.unity` existing, +5 baru: `ReflexDuel.unity`, `AirHockey.unity`, `WallDefense.unity`, `KeepUpDuo.unity`, `Battleship.unity`) | `<GameId>Controller` per scene (`ConnectFourController` existing pattern) | HUD umum: `Text_Turn`/`Text_Score`, `Grid_EmoteWheel` (6+16 tombol emote), `Text_ConnectionIndicator`; per-game board (lihat detail di bawah) | game-specific asset section 2-7 asset-list |
| S7 | `Result.unity` (baru) | `ResultController` | `Text_ResultHeadline`, `Text_LedgerDelta`/`Text_DuoScoreDelta`, `Text_SeriesCounter`, `Btn_Rematch`/`Btn_NextGame`/`Btn_Leave`, `Text_OpponentDecision` | — |
| S8 | `Profile.unity` (baru) | `ProfileController` | `TabBar_VersusCoop`, `List_Pairs` (ScrollView, row template = pasangan + badge), `Panel_PairDetail` (head-to-head per game) | SH-15 tab_bar, SH-32/33 badge_* |
| S9 | `Shop.unity` (baru) | `ShopController` | `Text_TicketBalance`, `Btn_WatchAd`, `Grid_Catalog` (ScrollView + `GridLayoutGroup`, 19 item), `Btn_BuyPremium` | SH-20..27 (avatar/frame/emote), C4-02/AH-02/BS-02 (skin), SH-26 victory_anim |
| S10 | `Settings.unity` (baru) | `SettingsController` | toggle sound/music/vibration, `Btn_RestorePurchase`, link privacy/ToS, `Text_Version` | — |
| S11 | `AsyncMatches.unity` (baru) | `AsyncMatchesController` | `List_AsyncMatches` (ScrollView, row = `your_turn_banner` + opponent + deadline countdown), tap row → `ResumeAsyncMatch` → `AppStateMachine.ToGame()` | BS-08 your_turn_banner |

**Detail S6 per game (board root di bawah `Screen_Game`, HUD sama seperti `ConnectFourController` existing pola grid+tap-zone):**

- **ConnectFour** (existing, **EXTEND** — grid 7x6 dan input kolom tetap): TAMBAH `Ring_TurnTimer` (C4-07, tampil mode live: 30s per giliran, AC-C4-06), HUD emote wheel umum (seperti game lain), handler `MatchWentAsync` (toast "Match continues in Async" → kembali Home, badge S11), dan render giliran-hangus (turn pindah tanpa disc baru).
- **ReflexDuel**: `Text_WaitState` ("Wait for it..."), `Image_SignalGo` (RD-02, muncul saat `phase=RD_ARMED`), `TapZone_Self` (Button fullscreen bawah), `Row_RoundPips` (5x RD-04 pip), `Panel_ReactionResult` (RD-06, tampil ms kedua pemain).
- **AirHockey**: `Image_Table` (AH-01), `Image_MalletSelf`/`Image_MalletOpponent` (drag via `EventTrigger` OnDrag → kirim `AirHockeyInput`), `Image_Puck`, `Text_ScoreTimer` (AH-06).
- **WallDefense**: `Image_Arena` (WD-01), `Image_PaddleSelf`/`Image_PaddlePartner` (drag 1-sumbu), `Pool_Balls` (template WD-03 di-clone runtime dari state `balls` — pola populate-from-template, template inactive sibling, BUKAN di dalam container yang di-`Destroy()` tiap frame), `Row_Lives` (5x WD-04), `Banner_Wave` (WD-05).
- **KeepUpDuo**: `Image_Ball` (KU-02), `Image_PaddleSelf`/`Image_PaddlePartner` (KU-03), `Glow_TurnIndicator` (KU-04, nyala di sisi yang WAJIB sentuh berikut, dari `last_toucher_seat`), `Text_Combo` (KU-05).
- **Battleship**: dua sub-panel — `Panel_Placement` (`Grid_MyFleet` 10x10 + `Btn_RandomPlacement` BS-07, drag kapal BS-03) dan `Panel_Firing` (`Grid_TargetOpponent` 10x10 pakai `marker_hit`/`marker_miss`/`marker_sunk` BS-04, `Crosshair_Aim` BS-05 di posisi tap terakhir).

---

## 6. State machine & game flow

### 6.1 Matchmaking + voting (item 3) — server `Pairing` actor, GDD §6.1

```
IDLE --(JoinQueue paired / JoinRoom matched)--> PAIRED
PAIRED --(auto)--> VOTING [15s timer]
VOTING --(kedua vote sama)--> LOCKED
VOTING --(kedua vote beda)--> SHOWDOWN [10s timer]
VOTING --(timeout, 1 vote)--> LOCKED (pilihan yang vote)
VOTING --(timeout, 0 vote)--> LOCKED (random weighted, RD x3)
VOTING/SHOWDOWN --(salah satu leave)--> CANCELLED
SHOWDOWN --(ShowdownPick salah satu)--> LOCKED
SHOWDOWN --(timeout)--> LOCKED (random di antara 2 kandidat)
LOCKED --(auto)--> COUNTDOWN [3s, client-local dari VotingLocked.countdown_ms]
COUNTDOWN --(auto)--> IN_GAME (Service.StartMatch dipanggil, MatchFound+GameStart terkirim seperti v0)
```

Jalur **vs Bot** (GDD S2 → S5 picker) MELEWATI seluruh state machine ini: `StartBotMatch{game_id, tier}` dari status idle → server validasi game+tier, pilih bot random dari `bot_profile` (availability game tsb), `UpsertBotPlayer`, langsung `Service.StartMatch` → `MatchFound`+`GameStart`. Tanpa `PairFound`, tanpa voting, tanpa countdown server-side (client boleh menampilkan countdown 3s lokal untuk konsistensi rasa).

### 6.2 Rematch carousel (item 3 turunan, GDD §6.2) — di dalam `Runner`

```
RESULT_SHOWN --(auto)--> DECIDING [20s timer, RematchStatusUpdate broadcast tiap keputusan masuk]
DECIDING --(keduanya REMATCH_SAME_GAME)--> REMATCH (Runner.startGame() ulang, game SAMA, series counter lanjut — client-side counter, tidak disimpan server)
DECIDING --(ada NEXT_GAME dari salah satu, keduanya sudah submit)--> NEXT_GAME (Runner exit onExit(true) -> Pairing baru untuk pasangan sama, VOTING lagi, series counter reset)
DECIDING --(ada accept=false / timeout 20s)--> DISSOLVED (Runner exit onExit(false), broadcastError rematch_declined/rematch_timeout — pesan v0 tidak berubah)
```

### 6.3 Room / invite (item 6, GDD §6.3)

```
(Create) --> OPEN [expires_at = now+30min]
OPEN --(invitee JoinRoom, host masih connected)--> langsung pair (Matchmaker.pair path yang sama) --> PAIRED (§6.1)
OPEN --(host disconnect sebelum invitee join)--> RESERVED [TTL 30min tetap berjalan dari created_at]
RESERVED --(invitee JoinRoom, host OFFLINE)--> PENDING_HOST [expires_at = now+5min, override sisa TTL 30min]
  -> kirim RoomJoinPending ke host via FCM (your_turn-style push, tipe invitee_joined)
PENDING_HOST --(host reconnect dalam 5 menit)--> pair host+invitee --> PAIRED
PENDING_HOST --(5 menit habis)--> EXPIRED, invitee terima RoomExpired
RESERVED --(30 menit habis tanpa klaim)--> EXPIRED
```
Sweep goroutine (interval 1 menit, tambahan ringan konsisten dengan pola "single binary, graceful shutdown"): scan `room` WHERE `state IN ('open','reserved','pending_host') AND expires_at < now` → set `expired`, kirim `RoomExpired` ke sesi yang masih terhubung dan terkait (host untuk `open`/`reserved`, invitee untuk `pending_host`). Baris `expired` dibersihkan (`DELETE`) setelah 24 jam oleh sweep yang sama, supaya `room_code` bisa dipakai ulang (lihat catatan `uq_room_code_active` §3.2).

### 6.3b Deep link, install referrer, landing page (item 6 — GDD 6.3 "detail teknis di TDD")

- **App Link (app terinstall):** intent filter `https://<domain>/r/*` dengan `autoVerify` (butuh `assetlinks.json` ter-host di domain final — Blocker B2; sampai domain final, pakai custom scheme fallback `twoup://r/{code}` yang tidak butuh verifikasi). Unity: cek `Application.absoluteURL` di Boot (cold start) + subscribe `Application.deepLinkActivated` (warm start). Parser: path `/r/{CODE}` → `MatchContext.PendingRoomCode = CODE`. Setelah `ServerHello`: kalau `PendingRoomCode` terisi → `AppStateMachine.ToInvite()` + auto-kirim `JoinRoom{code}`, konsumsi field.
- **Install referrer (app belum terinstall):** URL Play Store di landing page: `https://play.google.com/store/apps/details?id=com.evermore.twoup&referrer=utm_source%3Dinvite%26utm_content%3D{CODE}`. First launch client: baca via Play Install Referrer library (`com.android.installreferrer:installreferrer:2.2`, Gradle dep di `mainTemplate.gradle`; akses dari C# lewat wrapper `AndroidJavaObject` di `Assets/Scripts/App/InstallReferrerReader.cs` — pure-C# parse-nya dipisah supaya headless-testable). Parse `utm_content` dari referrer string → `MatchContext.PendingRoomCode`, lalu set flag `PlayerPrefs "twoup.referrer_consumed"=1` (baca sekali saja; referrer API tidak menjamin nilai bertahan). Flow selanjutnya identik jalur App Link.
- **Landing page status (countdown "Room reserved for 29:12" + expired state):** halaman tetap static; satu `fetch` ke endpoint HTTP publik read-only baru di server yang sama: `GET /r/{code}/status` → `{"state":"open|reserved|pending_host|expired","expires_at_unix_ms":...}` (handler di `mux` existing sebelah `/healthz`; CORS `Access-Control-Allow-Origin` = domain landing; tidak mengekspos data pemain selain state room).

### 6.4 Async hibernasi + auto-forfeit (item 5, GDD AC-C4-07/AC-BS-05)

```
Runner (Live mode, C4/BS) --(disconnect grace 30s habis, §4.4)--> konversi Async:
  1. persist async_match_state{current_turn_player_id, turn_started_at=now, forfeit_deadline=now+48h, board_state=game.State().state}
     - KHUSUS BS_PLACEMENT (AC-BS-02: placement tanpa deadline): current_turn_player_id=NULL,
       turn_started_at=NULL, forfeit_deadline=NULL — kedua pemain beraksi serentak, bukan turn;
       deadline mulai dihitung saat kedua placement terkunci (giliran pertama dimulai)
  2. broadcast MatchWentAsync{match_id, game_id} ke participant yang MASIH terhubung
     (client keluar scene game -> Home + badge S11), lalu Detach mereka
  3. Runner goroutine EXIT (tidak forfeit — match tetap 'active' di matches, tapi tidak ada actor resident)
Sweep goroutine (interval 5 menit):
  - baris nudge_push_sent_at IS NULL AND turn_started_at IS NOT NULL AND turn_started_at < now-24h -> kirim FCM your_turn, set nudge_push_sent_at=now
  - baris forfeit_deadline IS NOT NULL AND forfeit_deadline < now -> REHYDRATE (Resumable.LoadState) singkat HANYA untuk menghitung forfeitResult+persistFinish (end_reason='forfeit_timeout'), lalu exit lagi (tidak butuh Participant hidup untuk forfeit)
  - baris deadline NULL (BS placement) SELALU dilewati kedua aturan di atas
Pemain buka S11 / tap push -> ListAsyncMatches / ResumeAsyncMatch:
  1. ResumeAsyncMatch{match_id} -> server REHYDRATE: game.Factory.New() + LoadState(players, board_state) -> Runner baru
  2. Attach() session yang resume; kirim MatchResumed{state=board_state, grace_seconds_remaining=0}
  3. Kalau lawan JUGA online saat itu (dua sesi live) -> Runner lanjut sebagai Live mode lagi (tanpa turn timer, GDD 6.1: "Mode async: tanpa turn timer" tetap berlaku meski kedua online)
  4. Move diterapkan, giliran pindah ke pemain yang TIDAK terhubung -> Runner exit lagi (kembali ke cold/hibernasi, ulangi dari atas)
```

### 6.5 Reconnect (live, non-async) — lihat §4.4 untuk tabel grace per game; state machine sesi:

```
statusInMatch --(WS close)--> parked[playerID] = {actor, graceUntil} ; timer(graceSeconds)
parked --(ClientHello baru, playerID sama, sebelum graceUntil)--> re-Attach ke actor SAMA, kirim MatchResumed, batalkan timer
parked --(timer habis)--> actor.HandleLeave(playerID) (forfeit ATAU, untuk C4/BS, konversi async §6.4 — bukan forfeit)
```

---

## 7. Test plan

### 7a. Server (Go, `go test ./...`, in-memory store — pola existing `internal/server/integration_test.go`)

| Test class/file | Method | Verifikasi |
|---|---|---|
| `internal/match/pairing_test.go` | `TestPairing_BothVoteSame_Locks` | 2 vote sama → `VotingLocked` dengan `game_id` yang benar, `MatchFound` terkirim setelah countdown |
| | `TestPairing_VotesDiffer_Showdown` | vote beda → `VotingShowdown` dengan 2 kandidat persis pilihan masing-masing |
| | `TestPairing_ShowdownPick_Locks` | `ShowdownPick` salah satu → `VotingLocked` dengan game yang dipilih |
| | `TestPairing_ShowdownTimeout_RandomLocks` | tanpa pick sampai deadline → `VotingLocked` salah satu dari 2 kandidat |
| | `TestPairing_OneVoteBeforeTimeout_AutoLocks` | 1 vote, timeout 15s → `VotingLocked` = pilihan yang vote |
| | `TestPairing_NoVotesTimeout_WeightedRandom` | 0 vote, timeout → `VotingLocked` salah satu dari 6 game (assert termasuk dalam catalog) |
| | `TestPairing_LeaveDuringVoting_Cancels` | leave saat VOTING → sisi lain terima `VotingCancelled{reason:"opponent_left"}` |
| `internal/match/runner_rematch_test.go` | `TestRunner_BothRematchSameGame_Restarts` | kedua `REMATCH_SAME_GAME` → `startGame()` dipanggil ulang, `match_id` baru, game SAMA |
| | `TestRunner_OneNextGame_ExitsToVoting` | satu `NEXT_GAME` satu `REMATCH_SAME_GAME` → `onExit(true)` dipanggil |
| | `TestRunner_Decline_Dissolves` | `accept=false` → `onExit(false)`, broadcastError `rematch_declined` |
| | `TestRunner_RematchStatusBroadcast` | tiap decision masuk → `RematchStatusUpdate` terkirim ke kedua sisi |
| `internal/game/battleship/battleship_test.go` | `TestBattleship_PerPlayerState_HidesOpponentFleet` | `StateFor(p1)` tidak memuat `my_fleet` milik p2 |
| | `TestBattleship_Placement_RejectsOverlap` | 2 kapal overlap → error, state tidak berubah |
| | `TestBattleship_Fire_HitSunkWin` | tembak semua sel kapal → `Sunk`, semua kapal → `Status()` finished dengan winner benar |
| | `TestBattleship_LoadState_Roundtrip` | `State()` lalu `LoadState()` di instance baru menghasilkan `StateFor` identik |
| `internal/game/reflexduel/reflexduel_test.go` | `TestReflexDuel_TapBeforeGo_FalseStart` | tap sebelum ARMED → round hilang untuk penap, tidak nambah skor |
| | `TestReflexDuel_BothFalseStart_RoundReplays` | keduanya false start → round diulang, skor tidak berubah |
| | `TestReflexDuel_NoInput5s_OpponentWins` | satu sisi tidak input 5s setelah GO → sisi lain menang round |
| | `TestReflexDuel_BestOf5_EndsAt3Wins` | sisi capai 3 win → `Status()` finished |
| `internal/game/airhockey/airhockey_test.go` | `TestAirHockey_MalletSpeedClamped` | input delta > cap → posisi mallet hasil clamp ke `MalletSpeedCapUnitsPerSec` |
| | `TestAirHockey_GoalDetection_ResetsPuckServe` | puck lewati garis gawang → skor +1, puck posisi tengah, serve ke sisi kebobolan |
| | `TestAirHockey_SuddenDeath_NextGoalWins` | skor seri di waktu habis → `sudden_death=true`, gol berikutnya langsung `Status()` finished |
| `internal/game/walldefense/walldefense_test.go` | `TestWallDefense_WaveCurve_MatchesFormula` | wave N → `ball_count`/`ball_speed` sesuai formula §4.7 |
| | `TestWallDefense_LivesZero_RunEnds` | lives habis → `Status()` finished, `co_op_score` terisi |
| `internal/game/keepupduo/keepupduo_test.go` | `TestKeepUpDuo_AlternationRule_RejectsSameToucher` | seat sama sentuh 2x beruntun → sentuhan kedua tidak dihitung, bola jatuh natural |
| | `TestKeepUpDuo_SpeedCurve_MatchesFormula` | speed di skor tertentu sesuai formula §4.7 |
| `internal/store/store_test.go` (shared suite: jalan di memory by default — `make test` tetap tanpa DB — dan di MySQL kalau `TEST_DSN` di-set) | `TestPairLedger_BotMatch_NotRecorded` | match dengan salah satu `is_bot=true` → tidak ada baris/update di `pair_ledger` |
| | `TestPairLedger_MilestoneThresholds` | `total_matches` mencapai 10/50/100 pada baris agregat → `milestone_tier` berubah tepat |
| `internal/match/room_test.go` | `TestRoom_HostOfflineBeforeJoin_BecomesReserved` | host disconnect sebelum invitee join → state `reserved`, tetap joinable |
| | `TestRoom_InviteeClaimsWhileHostOffline_PendingHostWindow` | invitee join saat host offline → state `pending_host`, `expires_at` = +5min dari saat itu |
| | `TestRoom_PendingHostExpiry_SendsRoomExpired` | 5 menit habis tanpa host reconnect → invitee terima `RoomExpired` |
| `internal/match/reconnect_test.go` | `TestReconnect_WithinGrace_ResumesSameRunner` | disconnect lalu ClientHello ulang < grace → `MatchResumed` dengan `match_id` sama |
| | `TestReconnect_GraceExpiry_C4ConvertsAsync` | grace C4 habis → match tetap `active`, `async_match_state` row muncul, TIDAK forfeit |
| | `TestReconnect_GraceExpiry_AirHockeyForfeits` | grace AH habis → forfeit (winner = sisi yang tinggal) |
| `internal/match/async_test.go` | `TestAsyncHibernate_RehydrateAppliesInput` | `LoadState` + `ApplyInput` pada instance baru menghasilkan state identik dengan kalau match tidak pernah hibernasi |
| | `TestAsyncSweep_ForfeitDeadlinePassed_AutoForfeits` | `forfeit_deadline` lewat → sweep memanggil forfeit, `matches.end_reason='forfeit_timeout'` |
| | `TestAsyncConvert_NotifiesRemainingPlayer` | konversi async → participant yang masih terhubung menerima `MatchWentAsync{match_id}` sebelum Runner exit |
| | `TestAsyncSweep_SkipsPlacementRows` | baris `forfeit_deadline` NULL (BS placement) tidak di-nudge dan tidak di-forfeit |
| `internal/match/turntimer_test.go` | `TestRunner_TurnTimerExpiry_SkipsWithoutMove` | timer 30s habis (C4 live) → giliran pindah, jumlah disc TIDAK bertambah, `GameState` baru ter-broadcast |
| | `TestRunner_ThreeConsecutiveSkips_Forfeits` | 3 giliran hangus beruntun pemain sama → forfeit pemain itu (AC-C4-06) |
| | `TestRunner_AsyncMode_NoTurnTimer` | match hasil resume async → timer 30s tidak aktif (AC-C4-07) |
| `internal/match/botmatch_test.go` | `TestStartBotMatch_SkipsVotingStartsTierBot` | `StartBotMatch{game_id, tier}` → `MatchFound`+`GameStart` langsung (tanpa `PairFound`), bot ber-tier benar, `players.is_bot=true` |
| `internal/match/reconnect_test.go` (lanjutan) | `TestReconnect_Reattach_ReceivesCurrentState` | `HandleReattach` menukar participant; session baru menerima `GameState` terkini, session lama tidak menerima apa pun lagi |
| `internal/store/store_test.go` (jalan di memory DAN mysql via shared suite) | `TestWallet_DailyAdCap_ResetsByDate` | klaim ke-6 hari yang sama → `ErrDailyCapReached`; hari berganti → counter reset |
| | `TestPairLedger_CoopIncrementsAggregateOnly` | `RecordCoopResult` menaikkan `total_matches` baris agregat tanpa menyentuh wins/draws (milestone gabungan GDD 5.1) |
| | `TestPurchaseCosmetic_RejectsInsufficientAndDuplicate` | saldo kurang / sudah dimiliki / premium_exclusive tanpa premium → error, saldo tidak berubah |
| | `TestRecordBotResult_UpdatesPersonalStats` | hasil vs bot → `bot_wins/losses/draws` naik, `player_bot_best` ter-upsert; `pair_ledger` TIDAK tersentuh |
| `internal/server/meta_test.go` | `TestGetProfile_ReturnsPairsAndBotStats` | `GetProfile` → `ProfileData` berisi pairs terurut `last_played_at` desc + stat bot |
| | `TestRegisterPushToken_Persisted` | `RegisterPushToken` → `players.fcm_token` terisi |
| | `TestRoomStatusEndpoint_ReturnsStateJson` | `GET /r/{code}/status` → JSON state+expiry benar; code tak dikenal → 404 |

### 7b. Client (Unity EditMode, `Assets/Scripts/Tests/EditMode/`) — hanya logika C# murni (server tetap authoritative, client TIDAK menduplikasi resolusi game)

| Test class | Method | Verifikasi |
|---|---|---|
| `GameCatalogTests` | `Find_ReturnsEntryByExactId` | `GameCatalog.Find("air_hockey")` mengembalikan entry yang benar; id tak dikenal → null |
| `VotingCountdownFormatterTests` | `Format_RoundsDownToWholeSeconds` | 2999ms → "3", 0ms → "0" (murni fungsi format, tidak ada logika keputusan) |
| `RoomCodeSanitizerTests` | `Sanitize_UppercasesAndStripsAmbiguousChars` | input campur huruf kecil + `0/O/1/I` → hasil sesuai alfabet `roomCodeAlphabet` server (`ABCDEFGHJKLMNPQRSTUVWXYZ23456789`) |
| `AsyncMatchListSorterTests` | `Sort_YourTurnFirst_ThenByDeadlineAscending` | daftar campur `your_turn=true/false` → true dulu, lalu urut `forfeit_deadline_unix_ms` naik |
| `ReflexDuelStateFormatterTests` | `FormatReactionMs_HandlesUncompensatedFlag` | (lihat catatan §9) format tampilan ms tidak crash pada nilai batas |
| `DeepLinkParserTests` | `Parse_ExtractsCodeFromHttpsAndCustomScheme` | `https://<domain>/r/ABC234` dan `twoup://r/ABC234` → `ABC234`; path lain → null |
| `InstallReferrerParserTests` | `Parse_ExtractsRoomCodeFromUtmContent` | `utm_source=invite&utm_content=ABC234` → `ABC234`; referrer organik tanpa utm_content → null |

---

## 8. Manual prerequisites

- **Package name Android:** SUDAH `com.evermore.twoup` (diputuskan sesi sebelumnya) — ini **menggantikan** GDD §10's "TODO cek package name", GDD perlu di-update baris ini di revisi berikutnya (bukan blocker TDD, catatan sinkronisasi dokumen).
- **Firebase project + FCM:** buat project Firebase, unduh `google-services.json` untuk client (Android), buat service-account credential untuk server memanggil FCM HTTP v1 API (server butuh `FCM_SERVICE_ACCOUNT_JSON` env var baru, pola sama seperti `DSN` existing). Client butuh **Firebase Unity SDK (Messaging)** — import manual (unitypackage/UPM registry Google), termasuk External Dependency Manager (EDM4U) untuk resolusi Gradle.
- **SDK monetisasi client:** **Google Mobile Ads Unity plugin** (rewarded ads S9) + **Unity IAP package** (`com.unity.purchasing`, SKU `premium_unlock`) — keduanya install manual + konfigurasi id AdMob/Play Billing.
- **Play Install Referrer:** dependency Gradle `com.android.installreferrer:installreferrer:2.2` via `mainTemplate.gradle` (aktifkan Custom Main Gradle Template di Player Settings) — §6.3b.
- **DigitalOcean single-region (D7):** provisioning region — **[DECIDED, non-blocking]** `sgp1` (Singapore), latency terdekat untuk target pasar Indonesia tersirat dari bahasa dokumen; ini keputusan ops, bukan arsitektur, bisa diubah tanpa mengubah TDD ini.
- **Play Console:** setup SKU `premium_unlock` ($2.99 dasar, cek harga regional saat setup — GDD §4, non-blocking untuk engineering, harga di-set di Play Console bukan kode), setup closed testing track.
- **Domain landing page:** BLOCKER, lihat §11 B2.
- **Asset produksi**, prioritas dari `asset-list.md` §Rekap (urutan produksi GDD §7): shell UI kit + Connect Four dulu (sudah ada), lalu Reflex Duel → Air Hockey → Wall Defense → Keep-Up Duo → Battleship. Font (`SH-17`/`SH-18`) dan 6 bot avatar (`SH-27`) dibutuhkan SEBELUM S5 Voting bisa di-authoring penuh (game card butuh font+art).
- **"2UP" trademark/nama Play Store:** riset masih terbuka (GDD §10) — tidak menggate implementasi, tapi menggate SUBMIT ke Play Store.

---

## 9. Handoff notes untuk `ccq-plan-unity`

**Penting:** `ccq-plan-unity` adalah planner Unity-only. Dari 8 item yang diminta di percakapan ini, **item 1 (proto), 2 (MySQL), 3 (matchmaking server), 4 (bot server), 5 (async+FCM server), 6 (invite server), 7 (reconnect server), 8 (metrik server) mayoritas adalah pekerjaan Go/server** (lihat tabel §2 "apa yang berubah"). `ccq-plan-unity` hanya akan menghasilkan task untuk potongan CLIENT di bawah ini — sisanya (hampir semua §4 server-side) butuh rencana Go terpisah (di luar cakupan skill ini), TIDAK otomatis ter-cover.

**Yang jadi input `ccq-plan-unity` (client Unity saja):**
- **Task pertama (serial, sebelum semua):** update `CLAUDE.md` client — scope guard walking skeleton ("no second game, no deep links, no IAP, no ads, no audio") sudah usang dan AKAN dibaca worker; ganti dengan scope MVP GDD + rujuk TDD ini. Sekalian catat retirement `Lobby.unity`/`LobbyController`/`BuildLobbyScene()`.
- 14 scene baru §5 (Home, InviteRoom, Queue, Voting, ReflexDuel, AirHockey, WallDefense, KeepUpDuo, Battleship, Result, Profile, Shop, Settings, AsyncMatches) + extend `ConnectFourController` (§5 detail C4) /`NetworkClient`/`AppStateMachine` (§3.3)/`MatchContext` untuk event baru §3.1.
- `GameCatalog` ScriptableObject (§3.3, termasuk `sceneName`) + seed data 6 entri; `AppStateMachine.ToGame(gameId)` routing dari catalog.
- Meta screens S8/S9/S10 memakai kontrak §3.1.7 (GetProfile/GetShop/ClaimAdTicket/PurchaseItem/SetProfile/PremiumPurchased).
- Deep link + install referrer (§6.3b): `DeepLinkParser` + `InstallReferrerParser` (pure C#, §7b) + `InstallReferrerReader` (AndroidJavaObject wrapper) + konsumsi `MatchContext.PendingRoomCode` pasca-ServerHello.
- FCM: Firebase Messaging init di Boot + kirim `RegisterPushToken` setelah ServerHello dan pada token refresh; handler notification-tap → `ResumeAsyncMatch` (your_turn) / `ToInvite` (invitee_joined).
- Helper class murni C# di §7b (headless testable) — tulis DAN test bareng, jangan pisah fase.
- `NetworkClient.SendRateLimited` (§4.7) untuk 3 game real-time.
- Ping interval kondisional 2s selama `reflex_duel` aktif (§4.7).

**Urutan fase disarankan (logika dulu, front-load parallel):**
1. Proto regen (client `tools/generate-protos.ps1` setelah proto server di-extend) — SERIAL, blocking semua yang lain.
2. `NetworkClient` event baru (voting/rematch-status/async/room/emote) — SERIAL setelah 1, blocking semua controller baru.
3. `GameCatalog` + `MatchContext` extend — paralel dengan 4-9 setelah 2 selesai.
4-9. 10 scene baru — PARALEL antar scene (tidak saling bergantung setelah NetworkClient siap), TAPI tiap scene sendiri: script controller dulu (headless-testable bagiannya), scene authoring (visual, serial per-screen karena `SkeletonBuilder.cs` adalah SATU file — edit berbarengan oleh banyak task akan konflik; jadikan visual authoring untuk 10 scene ini SERIAL meski logikanya paralel).
10. EditMode test §7b — bareng dengan helper class terkait (bukan fase terpisah di akhir).

**Task visual yang serial (constraint konkret untuk planner):** semua penambahan ke `Assets/Editor/SkeletonBuilder.cs` (metode `Build<Screen>Scene` baru) harus SATU task berurutan per screen, karena file yang sama di-edit; JANGAN fan-out paralel pada file itu.

---

## 10. Changelog

- v1 2026-07-15: initial, dari GDD.md v1 + asset-list.md v1 + pembacaan langsung repo server `10cc70b` / client `e1ccd0a`.
- v1.1 2026-07-16: revisi hasil review. Ditambah: kontrak meta-systems §3.1.7 (Profile/Shop/Settings/push token), `StartBotMatch` (vs Bot skip voting) + `MatchWentAsync`, `PairFound` bawa badge data, extension `store.Store` lengkap (§4.8), instrumentasi metrik (§4.9), turn timer C4 via `TurnBased` + `GameRules` (AC-C4-06), `HandleReattach` (reconnect seat swap), deep link/install referrer/landing status endpoint (§6.3b), keputusan kamera Wall Defense DITULIS di §4.7 (render identik — memperbaiki referensi dangling B3 v1), schema: +`player_bot_best`, kolom players (avatar/frame/premium/is_bot/bot stats), `matches.coop_score`, `match_players.result` +`'coop_end'`, `end_reason` tanpa `'rematch_declined'`; `Factory.NewBot(self, tier)`, `RematchWindow` default 30s→20s, `ReflexDuelInput` tanpa client timestamp, semantik BS placement di async store, milestone versus+co-op digabung di baris agregat, routing scene via `GameCatalog.sceneName`, spec `AppStateMachine` baru, retirement `Lobby.unity`, test plan +20 kasus, prasyarat SDK (Firebase/Ads/IAP/Install Referrer).

---

## 11. Blockers eksplisit (butuh keputusan produk baru, tidak diputuskan sepihak di TDD ini)

- **B1 — Kontrol Keep-Up Duo (paddle-drag vs area-sentuh).** GDD §10 secara eksplisit menitipkan ini ke fase design, BUKAN ke TDD. `KeepUpDuoInput`/`KeepUpDuoState` (§3.1.6) memakai model paddle-drag 1D sebagai **default engineering** supaya proto/bot/scene bisa mulai dikerjakan — TAPI kalau fase design memilih area-sentuh (tap zone, bukan drag posisi kontinu), `KeepUpDuoInput` perlu field berbeda (mis. `int32 touch_zone` bukan `float paddle_x`), `KU-03` asset shape berubah, dan bot Hard's "arahkan pantulan enak" (§4.6) perlu re-derive dari titik sentuh bukan posisi paddle. **Perlu keputusan sebelum S6 KeepUpDuo di-authoring visual (bukan blocking untuk proto/server/bot logic scaffolding).**
- **B2 — Domain landing page.** GDD §6.3 dan §10 sama-sama eksplisit "TODO fase design/setup, kandidat 2upgames.com/r/, cek trademark". Deep link pattern `https://<domain>/r/{code}` (§3.1.4) sudah bisa diimplementasi dengan domain configurable (env var server + Android App Links `assetlinks.json` butuh domain final untuk verifikasi). **Blocking untuk: setup Android App Links (butuh domain final untuk `assetlinks.json` di server domain tsb), setup smart landing page hosting. TIDAK blocking untuk: proto, server room state machine, atau scene S3 (semua bisa dikerjakan dengan domain placeholder).**
- **B3 — Orientasi kamera Wall Defense** — SUDAH diputuskan di §4.7 (bullet "Orientasi render Wall Defense"): kedua client me-render orientasi **identik** (gawang bersama di bawah, TANPA mirroring per seat; identitas via warna paddle + label "YOU"). Bukan blocker; dicatat untuk audit trail bahwa GDD AC-WD-01 menyerahkan keputusan ini ke TDD dan sudah diambil (beda dari B1 yang sengaja ditahan untuk fase design). [Catatan review: draft v1 mengklaim keputusan "mirror per seat" yang tidak pernah ditulis di §4.7 — v1.1 menulis keputusan sebenarnya dan memilih render identik.]
- **B4 — Harga regional IAP `premium_unlock`.** GDD §4 eksplisit "cek per region saat setup Play Console" — tidak menggate kode (SKU id sudah fix), hanya menggate submit ke Play Console. Non-blocking untuk implementasi.
- **B5 — Nama/trademark "2UP" di Play Store.** GDD §10, riset masih terbuka. Non-blocking untuk implementasi (package name sudah aman diputuskan terpisah, `com.evermore.twoup`), blocking untuk submit final ke Play Store.
