# GDD: 2UP — 2 Player Games Online

Basis: research-summary.md (15 Juli 2026, revisi final). Keputusan D1-D9 dari research adalah constraint locked, tidak didebat ulang di dokumen ini.
Scope dokumen: produk dan game design. Netcode, schema persis, API, dan algoritma teknis dirujuk ke TDD.

---

## 1. Product overview & core loop

**Positioning:** koleksi minigame online 2 pemain (versus + co-op) untuk Android, invite-first, real-time arcade dengan game feel bagus. "GamePigeon untuk Android, tapi real-time arcade." Gameplay stat flat, tanpa pay-to-win (D6, D4, Section 4 research).

**Core loop (3 kalimat):**
1. Pemain mengajak teman via room code / deep link (utama) atau masuk queue (sekunder), dipasangkan, lalu berdua memilih game lewat voting.
2. Main satu match pendek (~1-5 menit), hasil match menambah rivalry ledger (versus) atau duo score (co-op) untuk pasangan tersebut.
3. Result screen mendorong rematch atau next game dengan pasangan yang sama; progres relasi (ledger, streak, duo level) adalah alasan kembali, ekonomi coin (daily reward + login streak membiayai match berikutnya) dan kosmetik adalah reward loop sekunder.

**Pilar desain:**
- P1: Relasi antar pasangan pemain adalah progression, bukan level individual (D5)
- P2: 6 game enak lebih baik dari 15 game biasa (Risiko #4 research)
- P3: Bot transparan, tidak pernah menyamar sebagai manusia (D4)
- P4: Komunikasi di-offload ke app eksternal; in-game hanya quick-chat emote

---

## 2. Screen map & navigasi

11 screen. Format: **Nama** | tujuan | dari → ke.

| # | Screen | Tujuan | Navigasi |
|---|--------|--------|----------|
| S1 | Splash/Boot | Logo, auth anonim device-based, load config | → S2 |
| S2 | Home | Hub utama: tombol Play with Friend, Quick Match, vs Bot, badge Async Matches, saldo coin, popup Daily Reward (claim login harian + streak), akses Profile/Shop/Settings | → S3, S4, S5(vs Bot), S8, S9, S10, S11 |
| S3 | Invite/Room | Buat room (code 6 char + tombol share deep link ke WhatsApp/Line/dll), atau join via code. Menunggu lawan | kedua pemain hadir → S5 |
| S4 | Queue/Pairing | Matchmaking single pool (D3). Timeout 10 detik → inject bot (D4) | paired → S5; cancel → S2 |
| S5 | Game Voting/Picker | Pasangan memilih game (flow di Section 6.1); tiap kartu game menampilkan biaya coin, kartu unaffordable disabled + shortcut "Get coins". Juga dipakai sebagai picker mode vs Bot (pilih game + tier bot langsung, gratis) | locked → S6 |
| S6 | Gameplay Container | Host scene per game + HUD umum (skor, series counter, emote wheel, indikator koneksi) | match selesai → S7; disconnect → S7 (forfeit state) |
| S7 | Result/Rematch Carousel | Hasil match, delta ledger/duo score, tombol Rematch / Next Game / Leave (flow di Section 6.2) | Rematch → S6; Next Game → S5; Leave/timeout → S2 |
| S8 | Profile & Ledger | Avatar preset + frame, stat pribadi, daftar rivalry (per pasangan) dan duo score. Tap pasangan → detail head-to-head per game | → S2 |
| S9 | Shop/Cosmetics | Katalog kosmetik, saldo coin, tombol rewarded ad ("Get Coins"), IAP 2UP Premium | → S2 |
| S10 | Settings | Sound/music toggle, vibration, restore purchase, privacy/ToS link, versi | → S2 |
| S11 | Async Matches | Daftar match async berjalan (Connect Four, Battleship): giliran siapa, deadline, resume | tap match → S6 |

Overlay (bukan screen): emote wheel (di S6), dialog konfirmasi leave, toast room expired, popup Daily Reward (di S2), overlay "Get coins" (rewarded ad, dipanggil dari S5/S7/S9 saat saldo kurang).

---

## 3. Sistem & data (kasar)

Entity dan field penting saja. Tipe persis, index, dan relasi ada di TDD (constraint D8: data layer N-player via daftar pemain + team_id, tidak ada asumsi 2 kolom pemain).

| Sistem | Entity | Field penting |
|--------|--------|---------------|
| Identity [MVP] | Player | player_id, display_name (generated, editable), avatar_id (preset), frame_id, created_at. Auth anonim device-based; account linking [Post-MVP] |
| Match [MVP] | Match, MatchPlayer | match_id, game_id, mode (versus/co-op), state, result; MatchPlayer: match_id, player_id, team_id, outcome, score (D8) |
| Rivalry ledger [MVP] | PairLedger | pair_id (unordered pair), game_id (+ baris agregat all-games), wins per sisi, draws, total_matches, streak_holder, streak_count, milestone tercapai, last_played_at. Lifetime (Section 5.1) |
| Duo score [MVP] | PairDuoScore | pair_id, game_id, best_score, total_coop_matches (agregat untuk duo level), last_played_at. Lifetime |
| Async turn [MVP] | AsyncMatchState | match_id, current_turn_player_id, turn_deadline, board_state ref (detail TDD) |
| Cosmetics [MVP] | CosmeticItem (static), PlayerInventory | item_id, type (frame/emote_pack/board_skin/victory_anim), price_coins, premium_exclusive flag; inventory: player_id, item_id, source (coin/premium/default) |
| Coin wallet [MVP] | CoinWallet | player_id, balance, streak_count, last_claim_date, daily_ad_count, last_ad_at |
| Bot roster [MVP] | BotProfile (static config) | bot_id, name, tier, avatar_id, per-game availability (Section 5.2) |
| Room/Invite [MVP] | Room | room_code, creator_id, reserved_for (dari deep link claim), state, expires_at (Section 6.3) |
| Push notification [MVP] | (service, bukan entity) | 2 tipe saja di MVP: `your_turn` (async) dan `invitee_joined` (room). Tipe lain [Post-MVP] |

---

## 4. Ekonomi coin & monetisasi (v1.2; constraint locked research tetap berlaku: tanpa pay-to-win, tanpa interstitial/banner)

Currency tunggal: **coin** (menggantikan ticket v1). Coin dipakai untuk (a) biaya main per match online dan (b) beli kosmetik. Analogi: arcade Timezone — tiap game punya harga sendiri sesuai durasi/bobot sesinya.

### 4.1 Biaya main per game [MVP]

| Game | Durasi tipikal | Biaya main (coin) |
|------|---------------|-------------------|
| Reflex Duel | ~1-1.5 menit | 1 |
| Keep-Up Duo | ~1-2 menit | 1 |
| Air Hockey | ~2-3 menit | 2 |
| Wall Defense | ~2-4 menit | 2 |
| Connect Four | ~3-5 menit (live) | 2 |
| Battleship | terpanjang (async multi-hari) | 3 |

Angka harga adalah nilai awal, tunable di fase balancing (remote config, lihat TDD).

Aturan penarikan:
- Biaya ditarik dari **masing-masing pemain** (kedua sisi bayar harga yang sama) saat match benar-benar mulai (masuk IN_GAME). Batal sebelum itu (lawan keluar saat voting/countdown) → tidak ada penarikan.
- **Rematch = match baru = bayar lagi.** Next Game = bayar harga game yang terpilih di voting berikutnya.
- **vs Bot / Co-op with Bot: GRATIS** — practice mode sekaligus safety net saat coin habis (pemain tidak pernah terkunci total dari gameplay).
- Game async (Connect Four async, Battleship): biaya ditarik **sekali saat match mulai**, bukan per giliran.
- Disconnect/forfeit di tengah match → tidak ada refund (match sudah dimainkan).
- Pemain baru mendapat **starting balance 20 coin** saat first launch (cukup ~10 match untuk onboarding).

### 4.2 Daily reward & login streak [MVP]

- Login pertama tiap hari (reset harian 00:00 waktu server) → popup Daily Reward di S2, claim 1 tap.
- Login berturut-turut menaikkan reward (streak): Hari 1 = 5, H2 = 6, H3 = 7, H4 = 8, H5 = 10, H6 = 12, H7+ = 15 coin/hari. Setelah hari ke-7, reward bertahan di 15/hari selama streak terjaga.
- Bolong 1 hari → streak reset ke Hari 1. Streak protection/freeze = [Post-MVP].
- Full streak 1 minggu = 63 coin ≈ 21-63 match tergantung game — daily reward adalah sumber coin utama; pemain yang login tiap hari praktis selalu bisa main.

### 4.3 Rewarded ads [MVP]

- 1 ad = **3 coin**. Cap 5 ad/hari (maksimal 15 coin/hari dari ads).
- Placement: tombol eksplisit "Get Coins" di S9, plus shortcut overlay "Get coins" saat saldo tidak cukup (kartu game unaffordable di S5, tombol Rematch/Next Game di S7, item terkunci di S9). Tetap tidak ada rewarded di tengah flow match.

### 4.4 IAP [MVP]

- **IAP tunggal** (tidak berubah): SKU `premium_unlock`, "2UP Premium", one-time $2.99 (harga final cek per region saat setup Play Console). Benefit: semua item kosmetik bisa di-claim langsung tanpa coin, plus frame eksklusif Golden Crown. Bukan unlock-all instan; pemain tetap claim per item (menjaga sense of collection).
- Premium **tidak menghapus biaya main** — biaya main tetap berlaku untuk semua pemain (menjaga ekonomi coin tetap berarti; benefit premium murni kosmetik, konsisten tanpa pay-to-win).
- IAP coin pack: tidak ada di MVP. Virtual economy/gifting: [Post-MVP], park sampai traction.

### 4.5 Katalog kosmetik MVP (harga di-reprice ke coin)

| Item | Tipe | Harga coin | Catatan |
|------|------|-----------|---------|
| 8 avatar frame | frame | 30 | 1 default gratis; Golden Crown khusus Premium |
| 4 emote pack (@4 emote) | emote_pack | 50 | 6 emote dasar gratis: 👍 😂 😮 😭 🔥 GG |
| Board skin Connect Four (2 varian) | board_skin | 40 | |
| Table skin Air Hockey (2 varian) | board_skin | 40 | |
| Grid skin Battleship (1 varian) | board_skin | 40 | |
| 2 victory animation | victory_anim | 60 | Dipakai di S7 saat menang versus |

Total 19 item unlockable + default (tidak berubah). Dengan income maksimal ~30 coin/hari (daily streak penuh + 5 ads) dikurangi biaya main, pace unlock tetap di kisaran ~3 bulan reward loop; angka reprice final divalidasi di fase balancing.

---

## 5. Relationship meta (jawaban TODO #1 dan #2 research)

### 5.1 Rivalry ledger (versus) [MVP]

- Kunci: pasangan pemain (unordered). Ledger per game_id plus satu baris agregat all-games.
- Format tampil: "You 7 : 5 Rina" per game, agregat di header pair detail. Plus streak aktif: "Rina on a 3 win streak 🔥".
- **Lifetime, bukan season.** Alasan: liquidity awal rendah (Risiko #1), reset season menghapus justru satu-satunya progres yang dimiliki pasangan sedikit match. Season overlay (ledger lifetime tetap, ranking season terpisah) = [Post-MVP].
- Milestone per pasangan (dari total_matches agregat, versus + co-op digabung): 10 match = badge "Rivals", 50 = "Arch Rivals", 100 = "Nemesis". Badge tampil di pair detail dan di S5 saat voting dengan pasangan itu.
- Match vs bot TIDAK masuk rivalry ledger (bot bukan relasi). Stat vs bot dicatat terpisah di profil pribadi (total menang/kalah vs bot).

### 5.2 Duo score (co-op) [MVP]

- Kunci: pasangan pemain, per co-op game: best_score (skor bersama terbaik) + total co-op match.
- Duo level per pasangan dari total co-op match (semua co-op game): Lv1 Rookie Duo (0), Lv2 Synced (5), Lv3 In Tune (15), Lv4 Dream Team (35), Lv5 Legendary Duo (75). Lifetime.
- Tampil: di S7 setelah co-op ("New duo best! 48 → 52"), di S8 pair detail, dan label duo level di S5.
- Co-op with Bot: skor tidak masuk duo score pasangan (tidak ada pasangan), masuk personal best "with Bot" terpisah.

### 5.3 Bot roster (jawaban TODO #2 research, constraint D4)

Semua bot tampil dengan nama personality + badge "AI" kecil, tidak pernah menyamar. Semua 6 game launch punya implementasi bot (tidak ada game yang di-exclude di MVP; interface `PlayerInput` tunggal per D4, detail di TDD).

| Bot | Tier | Kepribadian tampilan (emote behavior) |
|-----|------|--------------------------------------|
| Momo | Easy | Ramah, sering kirim 😂 dan 👍 |
| Zippy | Easy | Ceroboh, kirim 😭 saat kebobolan |
| Nova | Medium | Kalem, GG di akhir match |
| Rex | Medium | Kompetitif, 🔥 saat streak |
| Vega | Hard | Dingin, hampir tanpa emote |
| Apex | Hard | Arogan, 😮 saat lawan bikin play bagus |

- Queue timeout 10 detik → inject bot tier Medium (random Nova/Rex).
- Mode vs Bot / Co-op with Bot eksplisit di S5: pemain pilih game + tier (Easy/Medium/Hard), bot dalam tier dipilih random.
- Definisi kesulitan per tier per game ada di spec masing-masing game (Section 7), parameter angka final di TDD/balancing.

---

## 6. Flow detail (jawaban TODO #3 dan #4 research)

### 6.1 Game voting (D3) [MVP]

State: `PAIRED → VOTING → (LOCKED | SHOWDOWN) → COUNTDOWN → IN_GAME`.

1. **VOTING (15 detik):** kedua pemain melihat grid 6 game (kartu: ikon, nama, tag Versus/Co-op, tag Live/Turn-based, **harga coin**). Masing-masing tap satu. Pilihan sendiri tersorot, pilihan lawan tampil real-time.
   - Kartu dengan harga > saldo pemain tampil disabled + shortcut "Get coins" (overlay rewarded ad). Vote hanya bisa ke game yang mampu dibayar. Coin flip (rule 3) dan random server (rule 5) hanya memilih di antara game yang **kedua** pemain mampu bayar; jika irisan kosong → fallback ke game termurah yang kedua pemain mampu, dan jika tetap tidak ada, pairing dibatalkan dengan toast "Not enough coins" + CTA Get coins / vs Bot.
2. Kedua pilih sama → **LOCKED** → countdown 3 detik → masuk game.
3. Pilihan beda setelah keduanya vote → **SHOWDOWN (10 detik):** kedua kartu tersorot berdampingan, copy: "Pick one to agree, or we'll flip a coin!". Salah satu tap kartu lawan → LOCKED. Timeout → server random di antara dua pilihan (animasi coin flip).
4. Satu pemain tidak vote sampai 15 detik → pilihan pemain yang vote otomatis LOCKED.
5. Keduanya tidak vote → server pilih random dengan bobot ke game tercepat (Reflex Duel) supaya pasangan pasif tetap merasakan match.
6. Pemain keluar saat voting → lawan dapat toast "They left 😢" + tombol "Find new match" (kembali S4) / "Home".

UX copy kunci: header VOTING "Pick your game!", subheader "Both agree = instant start". Badge pasangan (milestone/duo level, Section 5) tampil di atas grid.

### 6.2 Rematch carousel (D5) [MVP]

State di S7: `RESULT_SHOWN → DECIDING (20 detik) → (REMATCH | NEXT_GAME | DISSOLVED)`.

1. Result menampilkan: hasil match, delta ledger ("You 7 : 5 Rina, streak +1") atau duo score, series counter jika sedang series ("Series 2 : 1").
2. Tiga tombol: **Rematch** (game sama, series counter lanjut; tombol menampilkan harga coin game, menarik biaya lagi dari kedua pemain), **Next Game** (kembali S5 voting dengan pasangan sama, series counter reset; biaya mengikuti game terpilih berikutnya), **Leave**. Saldo tidak cukup untuk Rematch → tombol disabled + shortcut "Get coins".
3. Status pilihan lawan tampil real-time: "Rina wants a rematch!".
4. Kedua Rematch → langsung countdown → game sama. Kedua Next Game, atau satu Rematch satu Next Game → S5 voting (Next Game menang karena voting bisa menghasilkan game sama juga). Ada Leave atau timeout 20 detik tanpa keputusan → pair dissolved: sisi yang tinggal dapat toast "Rina left. GG!" + CTA "Find new match" / "Invite a friend" / "Home".
5. Best-of series: tidak ada format formal best-of-N di MVP; series counter bebas berjalan selama rematch beruntun game yang sama, reset saat ganti gamehead. Format best-of-3 formal dengan stake = [Post-MVP].
6. Lawan bot: tombol dan flow sama, bot selalu setuju rematch/next game dalam 1-2 detik.

### 6.3 Invite flow + smart landing page (D2) [MVP]

- Room code 6 karakter alfanumerik uppercase tanpa karakter ambigu (0/O, 1/I).
- Deep link: `https://<domain>/r/{code}` (domain final: TODO fase design/setup, kandidat 2upgames.com/r/, cek bersama trademark). App terinstall → app link langsung buka S3 join. Tidak terinstall → smart landing page.
- **Landing page (static hosting):**
  - Headline: "{name} challenged you to {game}! 🎮" (game = pilihan inviter saat share, atau generik "a game of 2UP" jika share dari S3 tanpa konteks game)
  - Sub: "Quick 2-player games. Free on Android."
  - CTA: badge "Get it on Google Play" (install referrer membawa room code, detail teknis di TDD)
  - Countdown kecil TTL reservation: "Room reserved for 29:12"
  - Expired state: "This challenge expired 😅. Get 2UP and send one back!" + CTA Play Store tetap.
- **Room reservation TTL: 30 menit** sejak deep link dibuat (cukup untuk install + onboard; lebih lama = room zombie menumpuk). Setelah invitee masuk app dan claim room: jika inviter masih online di S3 → langsung pair → S5. Jika inviter sudah keluar → kirim push `invitee_joined` ke inviter, room bertahan **5 menit** lagi sebagai join window; lewat itu → expired, invitee dapat toast "Room expired. Challenge them back!" + shortcut buat room baru.
- Random matchmaking (S4) adalah jalur bonus, bukan positioning (D2). Tombol "Play with Friend" lebih prominent daripada "Quick Match" di S2.

---

## 7. Per-game spec (acceptance criteria)

Konvensi: AC ditulis testable. `[SKEL]` = sudah/sedang dicover walking skeleton (jangan diubah, harus match yang jalan). `[MVP]` = wajib launch di atas skeleton. `[Post-MVP]` = eksplisit di luar scope launch. Semua game: server-authoritative, input via interface `PlayerInput` tunggal untuk human dan bot (D4, detail TDD). Emote wheel aktif di semua game, cooldown kirim 3 detik.

### 7.1 Connect Four (versus, turn-based) — walking skeleton

Urutan produksi #1 (D1). Spec inti match dengan skeleton yang sedang jalan.

- [SKEL] AC-C4-01: Papan 7 kolom x 6 baris, mulai kosong, pemain pertama ditentukan server secara acak dan dikomunikasikan ke kedua client.
- [SKEL] AC-C4-02: Pada gilirannya, pemain memilih 1 kolom yang belum penuh; disc jatuh ke baris kosong terendah kolom itu. Move di luar giliran atau ke kolom penuh ditolak server dan state client tidak berubah.
- [SKEL] AC-C4-03: 4 disc segaris milik satu pemain (horizontal/vertikal/diagonal) → pemain itu menang, match berakhir, kedua client menampilkan hasil identik.
- [SKEL] AC-C4-04: 42 disc terpasang tanpa pemenang → draw.
- [SKEL] AC-C4-05: Kedua device menampilkan board state identik setelah setiap move (sinkron end-to-end 2 device fisik).
- [MVP] AC-C4-06: Mode live (kedua online): turn timer 30 detik; habis → giliran hangus, disc TIDAK dijatuhkan acak, giliran pindah; 3 giliran hangus beruntun oleh pemain yang sama → forfeit.
- [MVP] AC-C4-07: Mode async: tanpa turn timer per giliran; deadline 24 jam per giliran, push `your_turn` saat giliran tiba; 48 jam tanpa move → auto-forfeit. Match muncul di S11.
- [MVP] AC-C4-08: Disconnect di mode live: grace 30 detik reconnect ke state terkini; lewat itu match dikonversi ke async (bukan forfeit instan).
- [MVP] AC-C4-09: Hasil (win/lose/draw) tercatat ke rivalry ledger pasangan maksimal saat kedua client menampilkan S7.
- [MVP] AC-C4-10 (bot): Easy = kadang mengabaikan ancaman lawan; Medium = selalu blok ancaman 4 langsung dan ambil kemenangan langsung; Hard = look-ahead beberapa langkah (parameter di TDD). Bot bergerak dengan delay natural 1-3 detik.
- [Post-MVP]: board skin animasi, spectate.

### 7.2 Reflex Duel (versus, real-time) — urutan produksi #2

Validasi input relay dasar (D1). Sesi ~60-90 detik.

- [MVP] AC-RD-01: Match = best-of-5 round; pemain pertama mencapai 3 round win menang.
- [MVP] AC-RD-02: Tiap round: layar "Wait for it..." selama delay acak 1.0-4.0 detik (delay ditentukan server, identik untuk kedua pemain), lalu sinyal GO muncul serentak.
- [MVP] AC-RD-03: Tap pertama setelah GO (timestamp diadjudikasi server, detail kompensasi latency di TDD) memenangkan round; kedua client menampilkan selisih waktu ("You: 214 ms, Rina: 297 ms").
- [MVP] AC-RD-04: Tap sebelum GO = false start, pemain itu kalah round seketika, ditampilkan "Too soon!".
- [MVP] AC-RD-05: Kedua false start di round yang sama → round diulang, tidak ada skor.
- [MVP] AC-RD-06: Tidak ada input 5 detik setelah GO dari satu pemain → pemain lain menang round.
- [MVP] AC-RD-07: Disconnect: grace 10 detik; gagal reconnect → forfeit (game terlalu pendek untuk konversi async).
- [MVP] AC-RD-08 (bot): waktu reaksi bot disampel dari rentang per tier: Easy 450-800 ms, Medium 280-450 ms, Hard 190-280 ms; Easy false start ~10% round.
- [Post-MVP]: varian sinyal (suara, bentuk berbeda, sinyal palsu).

### 7.3 Air Hockey (versus, real-time) — urutan produksi #3

Validasi physics sync + interpolation (D1). Sesi ~2-3 menit.

- [MVP] AC-AH-01: Meja portrait, tiap pemain mengontrol 1 mallet di setengah lapangannya via drag; mallet tidak bisa melewati garis tengah.
- [MVP] AC-AH-02: Gol = puck sepenuhnya melewati garis gawang lawan; skor bertambah 1, puck respawn di tengah dengan serve ke pemain yang kebobolan.
- [MVP] AC-AH-03: Menang = pertama mencapai 7 gol, ATAU skor tertinggi saat waktu 150 detik habis; seri saat waktu habis → sudden death (gol berikutnya menang).
- [MVP] AC-AH-04: Physics puck-mallet-dinding server-authoritative; kedua client menampilkan posisi puck yang konsisten (toleransi interpolasi di TDD). Tidak ada gameplay stat yang bisa diubah item (flat, locked Section 4 research).
- [MVP] AC-AH-05: Disconnect: match pause maksimal 20 detik menunggu reconnect; gagal → pemain yang tersisa menang forfeit.
- [MVP] AC-AH-06 (bot): Easy = kecepatan mallet rendah, hanya reaktif; Medium = intersep lintasan puck; Hard = intersep + serangan terarah ke sudut. Kecepatan mallet bot di-cap sama dengan kecepatan maksimum drag manusia (fair by construction).
- [Post-MVP]: power puck temporer (harus tetap non-stat, hanya jika bisa dijaga fair), table skin tambahan.

### 7.4 Wall Defense (co-op, real-time) — urutan produksi #4

Reuse physics + netcode Air Hockey (D1). Sesi ~2-4 menit per run.

- [MVP] AC-WD-01: Kedua pemain masing-masing mengontrol 1 paddle di depan 1 gawang bersama (layar sama secara logis, tiap device menampilkan orientasi dari sisi gawang sendiri, detail kamera di TDD); wave bola AI datang dari arah berlawanan.
- [MVP] AC-WD-02: Shared lives = 5; bola masuk gawang → lives -1; lives 0 → run berakhir.
- [MVP] AC-WD-03: Skor bersama = jumlah bola yang berhasil ditepis + bonus per wave selesai; wave meningkat kecepatan dan jumlah bola (kurva di TDD/balancing).
- [MVP] AC-WD-04: Skor akhir dibandingkan dengan best duo score pasangan; jika lebih tinggi → best diperbarui dan S7 menampilkan "New duo best!".
- [MVP] AC-WD-05: Satu pemain disconnect: run pause 15 detik; gagal reconnect → run berakhir dengan skor saat itu (tetap dicatat).
- [MVP] AC-WD-06 (bot partner): Easy = cover hanya setengah area sendiri; Medium = cover area sendiri penuh; Hard = bantu cover celah pemain manusia.
- [Post-MVP]: power-up drop, boss wave.

### 7.5 Keep-Up Duo (co-op, real-time) — urutan produksi #5

Physics sederhana, shareable (D1). Sesi ~1-2 menit per run, retry loop cepat.

- [MVP] AC-KU-01: Satu bola di udara; pemain memantulkan bola dengan tap/drag paddle atau area sentuh (kontrol final di fase design); skor bersama +1 per sentuhan sah.
- [MVP] AC-KU-02: Aturan alternasi: pemain yang sama tidak boleh menyentuh bola 2 kali beruntun; sentuhan kedua beruntun tidak dihitung dan bola tetap jatuh natural (memaksa kerja sama).
- [MVP] AC-KU-03: Bola menyentuh lantai → run berakhir seketika (1 nyawa). Kecepatan bola naik bertahap seiring skor (kurva di TDD/balancing).
- [MVP] AC-KU-04: Skor akhir masuk duo score (aturan sama AC-WD-04).
- [MVP] AC-KU-05: Disconnect: run berakhir seketika dengan skor saat itu (run terlalu pendek untuk pause).
- [MVP] AC-KU-06 (bot partner): Easy = miss ~20% bola di areanya; Medium = miss ~5%; Hard = nyaris tidak miss dan memposisikan pantulan enak untuk manusia.
- [Post-MVP]: obstacle, mode target skor.

### 7.6 Battleship (versus, turn-based async) — urutan produksi #6

Pola terbukti GamePigeon (D1).

- [MVP] AC-BS-01: Grid 10x10 per pemain; fase placement: armada 5 kapal (panjang 5,4,3,3,2), horizontal/vertikal, tanpa overlap, tanpa keluar grid; placement tidak sah ditolak server; tombol random placement tersedia.
- [MVP] AC-BS-02: Placement tanpa deadline (async); match mulai saat kedua placement terkunci; pemain pertama acak dari server.
- [MVP] AC-BS-03: Per giliran 1 tembakan ke sel yang belum pernah ditembak; hasil Hit/Miss/Sunk (dengan nama kapal saat Sunk) ditampilkan ke kedua pemain; Hit TIDAK memberi giliran ekstra (klasik, sesi lebih pendek dan adil).
- [MVP] AC-BS-04: Menang = seluruh armada lawan tenggelam.
- [MVP] AC-BS-05: Async: deadline 24 jam per giliran, push `your_turn`; 48 jam tanpa move → auto-forfeit; muncul di S11. Kedua pemain online bersamaan → giliran berjalan live tanpa menunggu push.
- [MVP] AC-BS-06: Hasil tercatat ke rivalry ledger (aturan sama AC-C4-09).
- [MVP] AC-BS-07 (bot): Easy = tembakan acak murni; Medium = hunt/target (setelah Hit, tembak sel tetangga sampai Sunk); Hard = hunt/target + parity search. Bot melakukan move dalam 2-5 detik.
- [Post-MVP]: mode salvo (multi-tembak), armada custom.

---

## 8. Scope MVP vs Post-MVP (ringkasan)

**MVP (launch / closed testing):** 6 game Section 7 dengan AC [SKEL]+[MVP], 11 screen Section 2, voting + rematch carousel + invite flow Section 6, rivalry ledger + duo score + bot roster Section 5, ekonomi coin (biaya main per game + daily reward/login streak + rewarded ads) + monetisasi Section 4, 2 tipe push notif, English-only, Android-only, single region server (D7, detail infra di TDD).

**Post-MVP eksplisit (tidak dikerjakan sekarang):** Head Soccer-like (kandidat game #7), season overlay ranking, best-of-N formal, friends list persisten, web receiver, 3-4 pemain (UI/matchmaking; data layer sudah siap per D8), iOS, localization, leaderboard global, virtual economy/gifting, spectate, account linking, semua item [Post-MVP] di Section 7.

**Metrik gate closed testing (dari research):** match completion rate, persen match vs human, rematch rate per pairing. Instrumentasi event di TDD.

---

## 9. Referensi ke TDD (tidak dispesifikasikan di GDD ini)

Netcode & sinkronisasi (tick rate, interpolasi, latency compensation Reflex Duel), Protobuf message contract (D9), MySQL schema persis (D8), API/service contracts, algoritma bot per tier (parameter angka), reconnect/session resume, install referrer deep link, arsitektur server DigitalOcean (D7), instrumentasi metrik.

## 10. TODO titipan ke fase berikutnya

- Fase design: domain landing page final + visual landing page; kontrol final Keep-Up Duo (paddle vs area sentuh). Orientasi kamera Wall Defense **sudah diputuskan** di TDD.md §4.7 (kedua device render identik, gawang bersama di bawah, tanpa mirroring per seat).
- Fase setup: cek ketersediaan nama "2UP" di Play Store + trademark kasar (TODO research masih terbuka); harga regional SKU premium. Package name **sudah diputuskan**: `com.evermore.twoup` (lihat TDD.md §8).
- Fase TDD/balancing: kurva kecepatan Wall Defense & Keep-Up Duo, parameter bot per tier, toleransi interpolasi Air Hockey; validasi angka ekonomi coin (harga main per game §4.1, kurva daily reward §4.2, konversi ad §4.3, reprice kosmetik §4.5) — idealnya via remote config supaya tunable tanpa release.

## Changelog

- v1 2026-07-15: initial, dari research-summary.md revisi final (D1-D9 locked). Memutuskan: rivalry ledger & duo score lifetime + milestone/duo level, roster 6 bot 3 tier, flow voting/rematch/invite lengkap, landing page copy + TTL 30 menit/join window 5 menit, katalog kosmetik 19 item + model ticket.
- v1.1 2026-07-15: Section 10 diperbarui — package name Android diputuskan `com.evermore.twoup`, dipisah dari TODO nama/trademark "2UP" yang masih terbuka. Orientasi kamera Wall Defense dipindah dari titipan fase design ke "sudah diputuskan" (TDD.md §4.7: render identik di kedua device, tanpa mirroring per seat).
- v1.2 2026-07-17: **Ekonomi ticket → coin** (Section 4 ditulis ulang). Keputusan: (1) tiap game punya biaya main coin berbeda sesuai durasi (ala arcade Timezone, tabel §4.1); (2) biaya berlaku untuk semua match online termasuk rematch, **vs Bot gratis** (practice + safety net); (3) daily reward + login streak sebagai sumber coin utama (§4.2, H1=5 naik sampai H7+=15/hari, bolong = reset); (4) rewarded ads tetap, dikonversi 1 ad = 3 coin cap 5/hari (§4.3); (5) kosmetik di-reprice ke coin (§4.5), Premium tidak berubah dan tidak menghapus biaya main; (6) starting balance 20 coin. Turut diubah: screen map S2/S5/S9 + overlay, entity CoinWallet (ganti TicketWallet), flow voting §6.1 (affordability rule) dan rematch §6.2 (harga di tombol). Belum dicascade ke TDD/proto (lihat catatan implementasi).
