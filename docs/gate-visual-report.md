# Gate Visual Report — CHECKPOINT GATE VISUAL

Laporan verifikasi mekanis untuk gate "CHECKPOINT GATE VISUAL — semua scene terverifikasi mekanis" (`docs/plan.md`, task Fase 2). Semua nilai di bawah diisi dari hasil verifikasi nyata yang dijalankan ulang di task `t_0716_1527_bc1cd8c7` (2026-07-16), bukan disalin dari klaim task lama. Task ini murni dokumentasi — tidak ada `.cs`, `.unity`, `.asset`, atau `ProjectSettings` yang diubah.

## Tabel verifikasi per-scene

| Scene | File `.unity` ada | EditMode scene test hijau | Missing-script count = 0 | Terdaftar di EditorBuildSettings |
|---|---|---|---|---|
| Boot | Ya | Ya — `BaselineSceneTests.Boot_HasAppAndBootUi` | Ya (0) | Ya |
| Home | Ya | Ya — `HomeSceneTests` | Ya (0) | Ya |
| Invite | Ya | Ya — `InviteSceneTests` | Ya (0) | Ya |
| Queue | Ya | Ya — `QueueSceneTests` | Ya (0) | Ya |
| Voting | Ya | Ya — `VotingSceneTests` | Ya (0) | Ya |
| ConnectFour | Ya | Ya — `BaselineSceneTests.ConnectFour_Has42CellsAnd7Columns`, `ConnectFour_HasTimerRingAndEmoteWheel`, `ConnectFour_NoLegacyGameOverPanel` | Ya (0) | Ya |
| ReflexDuel | Ya | Ya — `ReflexDuelSceneTests` | Ya (0) | Ya |
| AirHockey | Ya | Ya — `AirHockeySceneTests` | Ya (0) | Ya |
| WallDefense | Ya | Ya — `WallDefenseSceneTests` | Ya (0) | Ya |
| KeepUpDuo | Ya | Ya — `KeepUpDuoSceneTests` | Ya (0) | Ya |
| Battleship | Ya | Ya — `BattleshipSceneTests` | Ya (0) | Ya |
| Result | Ya | Ya — `ResultSceneTests` | Ya (0) | Ya |
| Profile | Ya | Ya — `ProfileSceneTests` | Ya (0) | Ya |
| Shop | Ya | Ya — `ShopSceneTests` | Ya (0) | Ya |
| Settings | Ya | Ya — `SettingsSceneTests` | Ya (0) | Ya |
| AsyncList | Ya | Ya — `AsyncListSceneTests` | Ya (0) | Ya |

Catatan: Boot dan ConnectFour tidak punya kelas test khusus bernama `BootSceneTests`/`ConnectFourSceneTests` — keduanya diverifikasi lewat `BaselineSceneTests` (kelas test ke-15 dari 15 kelas scene test), yang secara eksplisit membuka dan menegaskan hierarki kedua scene tersebut. Ini bukan "n/a" — cakupan test untuk kedua scene ini nyata dan hijau, hanya digabung dalam satu kelas alih-alih dua kelas terpisah.

## Ringkasan hasil batchmode EditMode (dijalankan ulang di task ini)

- Perintah: `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\user\.ccq\bin\unity-guarded.ps1 EditMode`
- Hasil: **74/74 test lulus, 0 gagal, 0 inconclusive, 0 skipped**
- `result="Passed"`, `total="74"`, `passed="74"`, `failed="0"` (dari `.ccq-results-EditMode.xml`, node `test-run` id 2, durasi 0.60s)
- Log editor: `Test run completed. Exiting with code 0 (Ok). Run completed.` (`.ccq-unity-EditMode.log`)
- **Exit code: 0**
- Jumlah kelas scene test hadir: **15/15**, cocok persis dengan daftar wajib — `BaselineSceneTests`, `HomeSceneTests`, `InviteSceneTests`, `QueueSceneTests`, `VotingSceneTests`, `ResultSceneTests`, `AsyncListSceneTests`, `ProfileSceneTests`, `ShopSceneTests`, `SettingsSceneTests`, `ReflexDuelSceneTests`, `AirHockeySceneTests`, `WallDefenseSceneTests`, `KeepUpDuoSceneTests`, `BattleshipSceneTests`.

## Audit grep tambahan (dijalankan ulang di task ini)

1. `ProjectSettings/EditorBuildSettings.asset` — dibaca langsung: memuat **tepat 16 scene** (Boot, ConnectFour, Home, Invite, Queue, Voting, Result, AsyncList, Profile, Shop, Settings, ReflexDuel, AirHockey, WallDefense, KeepUpDuo, Battleship), **tanpa Lobby**. Cocok dengan daftar 16 scene yang diwajibkan AC.
2. `grep -c "m_Script: {fileID: 0}" Assets/Scenes/*.unity` (per-file, dijumlah) — **0 total** di semua 16 file scene.
3. `grep -rn "GameObject.Find(" Assets/Scripts/UI/` — **0 hit** (semua wiring UI lewat serialized field, bukan runtime `Find()`, sesuai `CLAUDE.md` konvensi).
4. `ls Assets/Scenes/*.unity | wc -l` — **16 file** `.unity` hadir di disk, cocok dengan 16 baris di `EditorBuildSettings.asset`.

## Visual polish pending design handoff

Item berikut diverifikasi dari isi scene builder / controller saat ini di `main` (bukan diasumsikan) dan masih menunggu design handoff (`CLAUDE.md`: *"Still no localization and no art pass — function over form until design handoff"*):

1. **WallDefense — ikon nyawa (`Row_Lives`)**: 5 elemen `Heart_0..4` di `WallDefenseSceneBuilder.cs` dibuat lewat `UiKit.CreatePanel` (panel warna merah polos 48x48), bukan sprite hati asli — placeholder kotak menunggu aset art.
2. **Settings — `Text_Links`**: teks statis `"Privacy Policy - Terms"` (`SettingsSceneBuilder.cs`) tanpa handler klik dan tanpa URL nyata — menunggu URL legal final dari design/legal handoff.
3. **ConnectionIndicator (dipakai di semua scene bertanding)**: status koneksi ditampilkan sebagai teks polos `"ON"` / `"reconnecting"` dengan perubahan warna (`ConnectionIndicator.cs`), bukan ikon status kustom — menunggu aset ikon dari design handoff.
4. **Seluruh 16 screen**: tidak ada localization (English-only, sesuai scope MVP) dan belum ada art pass menyeluruh (warna solid/placeholder untuk elemen seperti paddle, bola, glow turn-indicator, dsb. — lihat `docs/plan.md` bagian scene builder KeepUpDuo/WallDefense) — seluruhnya function-over-form sampai design handoff mendarat, per `CLAUDE.md` dan `docs/plan.md` baris "Scope guard".

Tidak ditemukan item lain di luar keempat kategori di atas pada pemeriksaan scene builder dan controller yang ada.

## Catatan manusia (opsional, bukan kriteria mesin)

Buka editor Unity, jalankan menu `2UP → Build All`, lalu spot-check hierarchy tiap scene secara visual di Game/Scene view untuk konfirmasi tambahan di luar assertion otomatis di atas.
