# IRONHOLD: ENDLESS SIEGE — UX, Polish & Feature Improvement Plan

> Companion to [`AAA_UPGRADE_PLAN.md`](AAA_UPGRADE_PLAN.md), which owns animation / combat / audio.
> **This doc owns:** UI/UX, controls & interaction, feel-smoothness, new features, performance, and the 2D art set.
> **Legend:** `[Priority · Effort · Impact]` → Priority `P0/P1/P2`, Effort `S/M/L`, Impact `Low/Med/High`.
> Architecture reminder: the whole game + UI is built at runtime in C# (no scenes/prefabs). UI edits live in
> `Assets/Scripts/UI/*` + `Bootstrap.cs` + `GameConfig.cs`; all UI motion must run on `Time.unscaledDeltaTime`.

---

## 0. Status — what this overhaul shipped vs. what's open

**Shipped this pass (✅):**
- Design-system + 9-slice UI (`UITheme`, sliced `Panel_Stone`, label outlines), central sprite registry (`UISprites`).
- Full **art set (18 assets)** generated with Higgsfield — refreshed panel + button, HUD status icons, style-tier badges, control glyphs, bar frame (see §2).
- **Safe-area** handling for notches / gesture bar (`SafeAreaFitter`, `UILayout`) across every screen.
- **Control redesign**: glyph icons, bigger thumb targets, held-state highlight (`UIIcons`, `TouchInputUI`).
- **Screen transitions** (`UITransition` fade+scale) on Menu / HUD / Pause / Game-Over / Settings.
- **Feel**: player accel/decel movement ramp, SFX pitch variance, announcer VO rate-limit, `SlashArc` on unscaled time, UI click SFX + haptic.
- **Settings / accessibility screen** (`SettingsService` + `SettingsController`): master/music/SFX volume, shake intensity, haptics — persisted via PlayerPrefs.
- **Perf**: removed per-hit `Material[]` alloc in `HitFlash` and per-swing sort closure in `MeleeHitbox`.

**Open / backlog (⬜):** enemy pooling (§7), between-wave economy (§6.2), guided tutorial (§6.4), meta-progression (§6.5), local leaderboard (§6.6), TMP font migration (§3), and the polish items tagged below.

**Cut-line:** §2 (art) + §3–§5 (UI/controls/feel) + §6.1 (settings) = the shippable "overhaul" and are done. Everything else is the ranked backlog.

---

## 1. Guiding pillars
- **Grim & readable** — dark stone/iron + night-blue; **ember-orange is the only signal color** (HP danger, stamina, style, wave, held state). Don't spend ember on decoration.
- **Tactile** — every input answers within one frame with visual + audio + (menu) haptic feedback.
- **One-thumb, landscape, glanceable** — nothing critical sits under a notch or the gesture bar; HUD reads at a glance.
- **Consistency** — one `UITheme` drives color/type/spacing; one art direction across all sprites (see §2 recipe).

---

## 2. Art & visual identity — the Higgsfield set  `[P0 · M · High]` ✅

Model: **Recraft V4.1** (`recraft_v4_1`). Palette locked to GameConfig hexes via the `colors` param. Pipeline per cutout:
`generate_image` → `remove_background` → downscale icons to 512². Authored on a flat `#E0E0E0` background for clean matting; verified composited on night-blue `#1B2233` (not white).

**Two art modes for cohesion:**
- `model_type: standard` + full 6-color palette → materic art (panel, button, status icons, tier badges, bar frame).
- `model_type: utility` + tight palette `[#4A4E57,#D8CFC0,#E8743B,#1B2233]` → clean readable control glyphs.

**Shared style prefix** (prepend to every prompt): *"Hand-painted stylized dark medieval-fantasy game UI art, painterly over faceted low-poly forms, dark wrought-iron & grey quarried-stone, warm ember-orange accents (#E8743B), cool night-blue shadow (#1B2233), bone-white highlights (#D8CFC0), muted desaturated, torchlit, crisp clean silhouette, no text."*

**The 18 shipped assets** (paths under `Assets/Resources/Art/UI/`):

| Asset | File | Mode | Notes |
|---|---|---|---|
| Stone panel (9-slice) | `Panel_Stone.png` | standard | replaced; ~150px border @1024, rendered ~38px (PPU 400) |
| Round button (transparent) | `Button_Round.png` | standard→cutout | replaced; kills the old baked grey box |
| Health / Stamina icons | `Icons/Icon_Health,Icon_Stamina` | standard→cutout | molten-core heart, ember flame |
| Coin / Wave icons | `Icons/Icon_Coin,Icon_Wave` | standard→cutout | coin doubles as future currency (§6.2) |
| Style-tier badges D→S | `Icons/Icon_Tier_{D,C,B,A,S}` | standard→cutout | iron→bronze→silver→gold→molten; letter drawn in code |
| Control glyphs | `Icons/Glyph_{Arrow,Punch,Sword,Block,Roll,Settings}` | utility→cutout | Arrow mirrored in code for LEFT |
| Bar frame | `Frame_Bar.png` | standard | opaque backing (available; not yet wired to bars) |

- Keep filenames stable → `Panel_Stone`/`Button_Round` drop in with zero code change; new icons wired via `GameConfig` path consts + `UISprites`.  `[P0 · S · High]` ✅
- **Backlog art (full set):** title-splash & app-icon refresh, wave-banner backing, pause vignette scrim, dedicated pause glyph.  `[P2 · M · Med]` ⬜

---

## 3. UI / UX redesign
- **Design system** — `UITheme.cs`: semantic colors (from the palette), type scale, outline/shadow, tap-target min, 9-slice + press-feel tokens.  `[P0 · M · High]` ✅
- **9-slice framing** — `RenderUtil.LoadSpriteSliced` + `Image.type=Sliced`; fixes the stretched stone frame on every button.  `[P0 · S · High]` ✅
- **Label legibility** — `UIFactory.Label` adds Outline/Shadow by default (skipped on alpha-animated labels: banner, combo, damage numbers).  `[P0 · S · Med]` ✅
- **Safe-area** — `SafeAreaFitter`/`UILayout.SafeAreaRoot`; full-bleed art stays on the root, interactive content insets. Critical in landscape where cutouts sit over HP (top-left) and pause (top-right).  `[P0 · S · High]` ✅
- **HUD icons** — health/stamina/coin icons + tier medallion behind the D/C/B/A/S readout.  `[P1 · M · Med]` ✅
- **TextMeshPro migration** — would sharpen text further but the project ships no TMP font asset/Essentials; deferred (build a dynamic SDF via `TMP_FontAsset.CreateFontAsset(RenderUtil.UIFont())` if pursued).  `[P2 · L · Med]` ⬜
- **Menu hierarchy QA** — verify title/subtitle/tagline/button spacing across 16:9→20:9; `CanvasScaler` match=0.5.  `[P1 · S · Med]` ⬜

---

## 4. Controls & interaction
- **Glyph icons** replace text symbols (`<` `>` `PUNCH`…); bigger thumb targets (move 168px, pause 96px).  `[P0 · M · High]` ✅
- **Held-state highlight** — ember ring on LEFT/RIGHT/BLOCK so holds vs taps are discoverable.  `[P1 · M · Med]` ✅
- **Press SFX + haptic** — wired `sfx_ui_button` + `Rumble.Light()` into `TouchButton` for menu-family buttons only (combat controls opt out).  `[P1 · S · Med]` ✅
- **Movement scheme** — kept the discrete `< >` pair (game is X-axis only; a joystick adds risk for no benefit). Confirmed decision, not a gap.
- **Left-handed layout toggle** — mirror the bottom-left/right clusters; add to Settings.  `[P2 · M · Med]` ⬜
- **Configurable button scale/opacity** — accessibility; extend Settings.  `[P2 · S · Med]` ⬜

---

## 5. Game-feel & smoothness
- **Movement accel/decel ramp** — `PlayerController._moveVel` via `Mathf.MoveTowards` (`GameConfig.PlayerMoveAccel/Decel`); kept on scaled time so it still freezes through hit-stop. Biggest "natural movement" win.  `[P1 · S · High]` ✅
- **SFX pitch variance** — `SfxManager` randomises 0.94–1.06 (tonal cues opt out); `BusVolume` for Settings.  `[P1 · S · Med]` ✅
- **Announcer VO rate-limit** — `AnnouncerVO` 4s cooldown on chatter; story/wave lines always interrupt.  `[P1 · S · Med]` ✅
- **SlashArc on unscaled time** — was freezing during the exact hit-stop it accompanies.  `[P1 · S · Med]` ✅
- **Screen transitions** — `UITransition` fade+scale (unscaled) instead of hard `SetActive` cuts.  `[P1 · M · High]` ✅
- **Wave-banner slide-in + ember flash**, **combo tier-up flourish** (badge + ember burst) — polish on top of the existing scale-pop.  `[P2 · S · Med]` ⬜

---

## 6. New features
### 6.1 Settings / accessibility  `[P0 · M · High]` ✅
`SettingsService` (PlayerPrefs, `IRONHOLD_*` keys) + `SettingsController` overlay, reachable from Menu + Pause: master/music/SFX volume, camera-shake intensity, haptics on/off; applied live and persisted. **Backlog:** reduce-motion, colorblind alt for the ember signal, text-size, hold-vs-toggle block, larger-buttons.  `[P1 · M · Med]` ⬜

### 6.2 Between-wave upgrades / economy  `[P1 · L · High]` ⬜
Hook `WaveManager` breather (`WaveBreather` 2.5s / `_inBreather`) → emit a `WaveCleared` event → open an upgrade panel. Spend score/coin (reuse `Icon_Coin`) on HP max, stamina, punch/sword damage, dodge i-frames, or a new move.

### 6.3 Tutorial / onboarding  `[P1 · M · High]` ⬜
Replace the static HOW-TO wall with a guided wave 1: just-in-time prompts ("tap SWORD", "hold BLOCK", parry-timing cue) driven off `CombatSystem` state.

### 6.4 Meta-progression  `[P2 · L · Med]` ⬜
Persistent currency across runs (PlayerPrefs, like `BestScoreKey`); unlock knight skins / starting perks / weapon variants.

### 6.5 Local leaderboard  `[P2 · M · Med]` ⬜
Top-10 (PlayerPrefs/JSON) — today only a single Best is kept; optional Google Play Games later.

---

## 7. Performance
- **HitFlash allocation** — precompute cached white/ember `Material[]` per renderer at `Init` (was `new Material[]` per renderer per hit).  `[P1 · M · Med]` ✅
- **MeleeHitbox closure** — cached `Comparison` + static origin instead of a per-swing lambda.  `[P2 · S · Low]` ✅
- **Enemy pooling** — `Spawner.Spawn` news a GameObject + async GLB every spawn and `WaveManager` Destroys on death → pool `EnemyBase` + reuse visuals. Largest remaining GC/spawn-hitch win (also flagged open in AAA §6.7).  `[P0 · L · High]` ⬜
- **MeleeHitbox layer mask** — dedicated actor layer instead of `~0` + `GetComponentInParent` per collider.  `[P2 · S · Low]` ⬜
- **Device tiering** — drop `MaxAliveHardCap` (14) to 8–10 on <4GB devices.  `[P2 · S · Med]` ⬜

---

## 8. Prioritized roadmap

| Milestone | Contents | New art? | Status |
|---|---|---|---|
| **M1 — UI Overhaul** | §2 art + 9-slice + glyphs + safe-area + transitions + HUD icons | 18 assets | ✅ done |
| **M2 — Settings & Feel** | §6.1 settings, §5 feel fixes, UI SFX/haptic | — | ✅ done |
| **M3 — Depth** | §6.2 between-wave economy, §6.3 tutorial | coin + panels | ⬜ |
| **M4 — Perf & Meta** | §7 enemy pooling, §6.4 meta, §6.5 leaderboard | — | ⬜ |

---

## 9. Open questions / risks
- **9-slice border** (`UITheme.PanelBorderPx`/`PanelSpritePPU`) is tuned to the authored frame; QA on the shortest buttons — raise PPU if corners overlap.
- **HUD layout** (icon/readout positions) was set without in-editor iteration — do a visual pass on a real device / Device Simulator.
- **Economy tuning** vs. the endless score curve — do upgrades flatten the difficulty?
- **Tier-badge letters** stay code-drawn (`ScoreManager.TierName`) — never bake text into AI art.

## 10. Appendix — asset → code touch-point map
- `GameConfig.cs` — path consts (`Glyph*`, `Icon*`, `TierBadgePaths[]`, `FrameBar`) + 9-slice/feel constants.
- `UISprites.cs` — `LoadAll()` loads every UI sprite once; consumers read the fields (null-safe).
- `Bootstrap.cs` — `UISprites.LoadAll()` + `SettingsService.LoadAndApply()` at startup; threads panel/button/title into `Build(...)`.
- `UIFactory.cs` / `UIIcons.cs` — `Icon`, sliced `PanelButton`, `SliderRow`/`ToggleRow`, `RoundButton` (glyph + held ring).
- `HUDController.cs` / `TouchInputUI.cs` — consume the glyph/icon sprites.
- New infra: `UITheme`, `UILayout`, `SafeAreaFitter`, `UITransition`, `SettingsService`, `SettingsController`.
