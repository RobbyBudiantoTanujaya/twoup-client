# Asset List: 2UP

Basis: GDD.md v1 (2026-07-15). Untuk fase design (Claude Design) dan produksi asset. Semua item [MVP] kecuali ditandai.

## Konvensi

- Format sprite: PNG transparan, power-of-two friendly, naming `snake_case`. Ukuran di bawah = target logical size @1x untuk referensi desain; export final @2x (detail import Unity di TDD).
- UI panel/button yang stretchable dibuat 9-slice.
- Audio: SFX .wav pendek (sumber), music loop .ogg. Semua audio original atau lisensi bebas royalti, tanpa IP eksternal.
- Style direction diputuskan di fase design; list ini hanya inventori kebutuhan.
- Ikon emote memakai custom sprite (bukan sistem emoji OS) supaya konsisten lintas device: `emote_thumbsup`, `emote_lol`, `emote_wow`, `emote_cry`, `emote_fire`, `emote_gg`.

---

## 1. Shell / UI global

### 1.1 Branding & identitas

| ID | Asset | Tipe | Deskripsi | Ukuran hint |
|----|-------|------|-----------|-------------|
| SH-01 | logo_2up | sprite | Logo utama, varian full + icon-only | 512x512 |
| SH-02 | app_icon | sprite | Play Store icon (adaptive: foreground + background layer) | 432x432 fg |
| SH-03 | splash_bg | sprite | Background S1 | 1080x1920 |
| SH-04 | feature_graphic | sprite | Play Store feature graphic | 1024x500 |

### 1.2 UI kit (dipakai semua screen)

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| SH-10 | btn_primary / btn_secondary / btn_danger | 9-slice | 3 state: normal, pressed, disabled |
| SH-11 | panel_card | 9-slice | Kartu umum (game card, list item, dialog) |
| SH-12 | panel_toast | 9-slice | Toast/snackbar |
| SH-13 | icon_set_ui | sprite sheet | back, close, settings, profile, shop, share, copy, sound_on/off, vibration, ticket, ad_play, timer, wifi_ok, wifi_bad, crown_premium, badge_ai | ~24 ikon 64x64 |
| SH-14 | input_field_code | 9-slice | Field room code 6 char |
| SH-15 | tab_bar | sprite | Tab/segmen (Profile: Versus/Co-op; Shop kategori) |
| SH-16 | bg_home | sprite | Background S2 (bisa reuse pattern untuk S3-S5, S7-S11) |
| SH-17 | font_display | font | Judul/logo-adjacent, pilih di fase design |
| SH-18 | font_ui | font | Body/angka, wajib legible untuk skor & timer |

### 1.3 Avatar & kosmetik

| ID | Asset | Tipe | Deskripsi | Jumlah |
|----|-------|------|-----------|--------|
| SH-20 | avatar_preset_01..12 | sprite | Avatar preset (karakter/ikon non-foto), 256x256 | 12 |
| SH-21 | frame_default | sprite | Frame avatar default gratis | 1 |
| SH-22 | frame_01..08 | sprite | 8 frame unlockable ticket | 8 |
| SH-23 | frame_golden_crown | sprite | Frame eksklusif Premium | 1 |
| SH-24 | emote_base_x6 | sprite | 6 emote dasar (lihat Konvensi), 128x128 | 6 |
| SH-25 | emote_pack_01..04 | sprite | 4 pack x 4 emote unlockable | 16 |
| SH-26 | victory_anim_01, victory_anim_02 | sprite sheet / skeletal | 2 victory animation untuk S7 | 2 |
| SH-27 | bot_avatar_momo/zippy/nova/rex/vega/apex | sprite | Avatar 6 bot, ekspresif sesuai kepribadian, 256x256 | 6 |

### 1.4 Game cards & meta visual

| ID | Asset | Tipe | Deskripsi | Jumlah |
|----|-------|------|-----------|--------|
| SH-30 | gamecard_c4/rd/ah/wd/ku/bs | sprite | Ilustrasi kartu per game untuk S5 & S11, 512x288 | 6 |
| SH-31 | tag_versus / tag_coop / tag_live / tag_turnbased | sprite | Badge kecil di game card | 4 |
| SH-32 | badge_rivals / badge_archrivals / badge_nemesis | sprite | Milestone pasangan (10/50/100) | 3 |
| SH-33 | badge_duolevel_1..5 | sprite | Duo level Lv1-Lv5 | 5 |
| SH-34 | anim_coinflip | sprite sheet | Animasi coin flip showdown voting | 1 |
| SH-35 | anim_matchfound | sprite sheet | Animasi paired di S4 | 1 |

### 1.5 Audio shell

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| SH-40 | music_menu_loop | ogg | Loop menu (S2-S5, S7-S11) |
| SH-41 | sfx_ui_tap / sfx_ui_back / sfx_ui_error | wav | Feedback UI dasar |
| SH-42 | sfx_match_found | wav | Paired di queue |
| SH-43 | sfx_vote_locked | wav | Game locked di voting |
| SH-44 | sfx_countdown_tick / sfx_countdown_go | wav | Countdown 3-2-1 + GO |
| SH-45 | sfx_win / sfx_lose / sfx_draw | wav | Stinger hasil match di S7 |
| SH-46 | sfx_duo_best | wav | Stinger new duo best |
| SH-47 | sfx_emote_pop | wav | Emote muncul |
| SH-48 | sfx_ticket_earn / sfx_item_unlock | wav | Shop feedback |
| SH-49 | music_gameplay_arcade_loop | ogg | Loop bersama game real-time (RD, AH, WD, KU) |
| SH-50 | music_gameplay_board_loop | ogg | Loop bersama game turn-based (C4, BS), lebih kalem |

### 1.6 Smart landing page (web, bukan Unity)

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| SH-60 | landing_hero | image | Hero ilustrasi 2 pemain, reuse gaya SH-30 |
| SH-61 | landing_og_image | image | Open Graph preview untuk share WhatsApp/Line, 1200x630 |
| SH-62 | badge_googleplay | image | Official Google Play badge (download dari brand guideline Google, bukan dibuat) |

---

## 2. Connect Four

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| C4-01 | board_c4_default | sprite | Papan 7x6 default (frame + lubang) |
| C4-02 | board_c4_skin_01 / _02 | sprite | 2 board skin unlockable |
| C4-03 | disc_p1 / disc_p2 | sprite | Disc 2 warna, high contrast, colorblind-safe |
| C4-04 | disc_drop_anim | sprite sheet / tween spec | Referensi animasi jatuh + bounce kecil |
| C4-05 | highlight_win_line | sprite | Penanda 4 disc pemenang |
| C4-06 | column_hover_indicator | sprite | Indikator kolom yang akan dipilih |
| C4-07 | turn_timer_ring | sprite | Ring timer 30 detik mode live |
| C4-A1 | sfx_disc_drop / sfx_disc_land | wav | Drop + mendarat |
| C4-A2 | sfx_c4_win_line | wav | Garis kemenangan menyala |

## 3. Reflex Duel

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| RD-01 | bg_reflex_wait | sprite | State "Wait for it..." (tension, gelap) |
| RD-02 | signal_go | sprite | Sinyal GO besar, kontras tinggi |
| RD-03 | tap_zone_p1 / tap_zone_p2 | sprite | Area tap per pemain (split screen indicator) |
| RD-04 | round_pip_empty / _won / _lost | sprite | Indikator round best-of-5 |
| RD-05 | falsestart_flash | sprite | Feedback "Too soon!" |
| RD-06 | reaction_time_panel | 9-slice | Panel hasil ms per round |
| RD-A1 | sfx_signal_go | wav | Sinyal GO (harus instan/tajam) |
| RD-A2 | sfx_round_win / sfx_round_lose / sfx_falsestart | wav | Feedback round |

## 4. Air Hockey

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| AH-01 | table_ah_default | sprite | Meja portrait default (garis tengah, lingkaran, gawang) |
| AH-02 | table_ah_skin_01 / _02 | sprite | 2 table skin unlockable |
| AH-03 | mallet_p1 / mallet_p2 | sprite | 2 mallet |
| AH-04 | puck | sprite | Puck + varian trail/glow saat cepat |
| AH-05 | goal_flash_anim | sprite sheet | Ledakan gol di gawang |
| AH-06 | score_display | sprite | Frame skor + timer 150 detik |
| AH-07 | serve_indicator | sprite | Arah serve setelah gol |
| AH-A1 | sfx_puck_hit_mallet / sfx_puck_hit_wall | wav | Impact, 2-3 variasi pitch |
| AH-A2 | sfx_goal / sfx_goal_against | wav | Gol untuk/kebobolan |
| AH-A3 | sfx_sudden_death | wav | Stinger masuk sudden death |

## 5. Wall Defense

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| WD-01 | bg_wall_defense | sprite | Arena + gawang bersama |
| WD-02 | paddle_p1 / paddle_p2 | sprite | 2 paddle beda warna (identitas pemain) |
| WD-03 | ball_wave_normal / ball_wave_fast | sprite | Bola AI, varian visual saat kecepatan naik |
| WD-04 | lives_heart_full / lives_heart_empty | sprite | Indikator 5 shared lives |
| WD-05 | wave_banner | sprite | Banner "Wave 3" antar wave |
| WD-06 | save_flash | sprite | Feedback tepisan berhasil |
| WD-A1 | sfx_wd_save / sfx_wd_concede | wav | Tepis / kebobolan (lives -1) |
| WD-A2 | sfx_wave_start / sfx_wave_clear | wav | Mulai / selesai wave |
| WD-A3 | sfx_run_over | wav | Lives habis |

## 6. Keep-Up Duo

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| KU-01 | bg_keepup | sprite | Latar + lantai (zona game over jelas) |
| KU-02 | ball_keepup | sprite | Bola utama + squash-stretch frames |
| KU-03 | paddle_ku_p1 / paddle_ku_p2 | sprite | Kontrol pantul per pemain (bentuk final tergantung keputusan kontrol fase design, lihat GDD 10) |
| KU-04 | turn_glow_p1 / turn_glow_p2 | sprite | Indikator siapa yang wajib menyentuh berikutnya (aturan alternasi) |
| KU-05 | combo_counter_pop | sprite | Angka skor pop tiap sentuhan |
| KU-A1 | sfx_bounce | wav | Pantulan, pitch naik seiring skor |
| KU-A2 | sfx_wrong_toucher | wav | Sentuhan tidak sah (pemain sama 2x) |
| KU-A3 | sfx_ball_drop | wav | Bola jatuh, run berakhir |

## 7. Battleship

| ID | Asset | Tipe | Deskripsi |
|----|-------|------|-----------|
| BS-01 | grid_bs_default | sprite | Grid 10x10 + label koordinat |
| BS-02 | grid_bs_skin_01 | sprite | 1 grid skin unlockable |
| BS-03 | ship_5 / ship_4 / ship_3a / ship_3b / ship_2 | sprite | 5 kapal, varian horizontal + vertikal (atau rotasi) |
| BS-04 | marker_hit / marker_miss / marker_sunk | sprite | Penanda tembakan |
| BS-05 | crosshair_aim | sprite | Bidikan sel |
| BS-06 | placement_valid / placement_invalid | sprite | Feedback taruh kapal (hijau/merah overlay) |
| BS-07 | btn_random_placement | sprite | Ikon dadu random placement |
| BS-08 | your_turn_banner | sprite | Banner giliran (dipakai juga notif visual di S11) |
| BS-A1 | sfx_shot_fire / sfx_shot_hit / sfx_shot_miss | wav | Tembakan |
| BS-A2 | sfx_ship_sunk | wav | Kapal tenggelam |
| BS-A3 | sfx_place_ship / sfx_place_invalid | wav | Placement |

---

## 8. Rekap jumlah

| Kategori | Sprite/visual | Audio |
|----------|--------------|-------|
| Shell/UI global | ~95 (termasuk 24 ikon, 12 avatar, 10 frame, 22 emote, 6 bot avatar, 6 game card) | 15 |
| Landing page (web) | 3 | 0 |
| Connect Four | 9 | 3 |
| Reflex Duel | 8 | 4 |
| Air Hockey | 10 | 5 |
| Wall Defense | 9 | 5 |
| Keep-Up Duo | 7 | 3 |
| Battleship | 14 | 6 |
| Font | 2 | |

Prioritas produksi mengikuti urutan produksi game (GDD Section 7): shell UI kit + Connect Four dulu (walking skeleton butuh C4-01, C4-03..07 + SH-10..13 minimal), lalu per game sesuai urutan D1.

## Changelog

- v1 2026-07-15: initial, diturunkan dari GDD.md v1.
