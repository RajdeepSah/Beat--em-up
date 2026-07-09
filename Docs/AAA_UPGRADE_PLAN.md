# IRONHOLD: ENDLESS SIEGE — AAA Character & Animation Upgrade Plan

*Drafted 2026-07-02 from a full code audit (player, core pipeline, enemies, presentation) plus a Higgsfield capability scan (678-clip rig library, 3D pipeline, workspace credits).*

> **STATUS 2026-07-03 — Phases 0, 1, 2 and the Phase 3 sound core are IMPLEMENTED.**
> - Phase 0: `Impact.cs` (hit-stop), trauma camera + look-ahead + kill punch-in, `HitFlash`,
>   `DamageNumbers`, `HitSparks`, impulse knockback, exp smoothing — all wired.
> - Phase 1: 23 skeletal clips generated via `3d_rigging` (8 cr each, 184 cr total), stripped
>   to 1.4 MB total in `StreamingAssets/Models/Anim/`, node-path-validated against every base
>   rig; `AnimLibrary` + `ActorAnimator` + debug clip cycler + procedural fallback live.
> - Phase 2: 0.35 s input buffer + cancel windows, jab→hook→uppercut chain + sword finisher,
>   dodge roll with i-frames (new ROLL button), knockdown/launch/OTG states, ember wind-up
>   telegraphs, `AttackDirector` 2-token crowd control — all in `CombatSystem`/`EnemyBase`.
> - Phase 3 (core): 19 synthesized SFX (`tools/gen_sfx.py`) + footsteps + block-raise wired.
> - **Polish tier (added 2026-07-03, second pass):** parry (0.15 s window, attacker punish
>   stagger, ping SFX), style-meter rework (hits/kills/parries feed D/C/B/A/S tiers → x1-x5,
>   one-tier loss on hit, HUD decay bar + tier letter + scale pop), HP damage-lag ghost bar,
>   low-HP vignette pulse, death slow-mo (0.25× + dolly), procedural sword slash arc,
>   Android haptic rumble, Brute head-down charge (clip 512, blockable → 1.5 s self-stagger),
>   Skeleton lunge-step, Grunt 20% feint, elites from wave 8 (1.15× scale + ember glow,
>   2× points), FRENZY every 5th wave, and a synthesized 2-layer adaptive music system
>   (`tools/gen_music.py` + `MusicManager`).
>   Still open: enemy pooling perf pass, settings toggles (shake/haptics), and measuring the
>   clip contact windows (ActiveStart/EndNorm) with the F9 clip cycler in `GameConfig` ClipDefs.

---

## 1. Where the game stands today

- **Unity 6000.5.1f1, Built-in RP, glTFast 6.19.0.** Fully code-first: `Bootstrap.cs` builds everything at runtime; GLBs load from `StreamingAssets/Models` via `GltfImport.Load` + `InstantiateMainSceneAsync`.
- **No skeletal animation is ever played.** The rigs inside the GLBs are never driven — `GlbLoader.AttachVisual` never adds an `Animation`/`Animator` component. All motion is whole-mesh transform tweening in `PlayerController.AnimateVisual()` (`PlayerController.cs:132-197`) and `EnemyBase.AnimateVisual()`: walking is a bob, attacking is a lean, limbs never move. Shipped GLBs carry only the baked Idle clip (action id 0).
- **The upgrade path was pre-planned:** `GameConfig.cs:116-126` already documents the Higgsfield clip ids intended for the swap (Walk 30, Punch 96, Sword 102, Block 138, Hit 178, Die 8).
- **Combat is a clean phase machine** (`CombatSystem`: Ready→Startup→Active→Recovery, read-only surface `Phase/PhaseT01/IsBlocking/IsStunned/CanAct`) with hard-coded timings in `GameConfig`. Damage is one instant `Physics.OverlapBox` at Active start (`MeleeHitbox.ApplyMelee`). Only a 0.10 s re-trigger buffer — no chains, dodge, parry, jump, knockdown.
- **Zero game feel:** no hit-stop, camera shake, particles, damage numbers, hit flash, haptics. **Zero combat SFX ship** (16 keys defined in `SfxManager`, folder contains only a README) and there is no music system. Camera is a static X-follow.
- **Enemy AI is 1-D chase-and-swing** with no group coordination; all three archetypes share one FSM; knockback is a single-frame teleport; death is a 0.7 s roll-and-sink then `Destroy`.
- **Higgsfield NSS workspace: 1,705 credits available** (already selected). Meshy rig library: 678 biped clips (154 in the Fighting group), each with a preview GIF; `3d_rigging` can re-rig an **existing GLB** with one clip per job — the key to a cheap moveset.

**Architecture verdict:** the codebase is unusually well-positioned for this. `AnimateVisual()` is the single seam for skeletal animation, `MeleeHitbox.ApplyMelee`'s ignored hit-count return and `GameManager.RegisterEnemyKilled` are the natural hooks for the entire juice layer, and CombatSystem needs **zero** changes for Phase 1.

---

## 2. The plan at a glance

| Phase | What | Needs assets? | Effort |
|---|---|---|---|
| **0 — Impact kernel** | Hit-stop, camera shake, hit flash, impulse knockback, damage numbers, hit sparks, camera look-ahead | No | ~1 session, pure code |
| **1 — Real skeletal animation** | Generate ~20-29 one-clip GLBs via Higgsfield `3d_rigging`, runtime clip harvesting + `ActorAnimator`, procedural fallback kept | Yes (≤ ~1,100 credits worst case) | 2-3 sessions |
| **2 — Combat interactivity** | Input buffer + cancel windows, 3-hit chain + heavy finisher, dodge roll w/ i-frames, knockdown/launch, enemy telegraphs + attack tokens | Uses Phase 1 clips | 2 sessions |
| **3 — Sound & presentation** | Generate all SFX + music, animation-synced audio, style meter, HUD juice, death slow-mo, trails, rumble, elites | Yes (~50-100 credits) | 1-2 sessions |

Phase 0 is deliberately first: it needs no assets, transforms the game immediately, and every later phase plugs into it. Higgsfield generation jobs (Phase 1/3 assets) are async — they can be submitted early and processed while Phase 0 code lands.

---

## 3. Phase 0 — Impact kernel (feel foundation, no assets)

All numbers go in `GameConfig.cs`.

1. **Fix frame-rate-dependent smoothing first** (`PlayerController.cs:195-196` + `EnemyBase`): replace per-frame `Lerp(0.5)`/`Slerp(0.4)` with `1 - Mathf.Exp(-k * Time.deltaTime)`, k=12 position / k=10 rotation. Every feel value below assumes this.
2. **`Impact.cs` static facade**, called from `CombatSystem.ApplyHit` (when hits>0), `EnemyBase.DoStrike`, `PlayerHealth.TakeDamage`, `GameManager.RegisterEnemyKilled`:
   - **Hit-stop** (unscaled-time coroutine, `Time.timeScale=0.05`): light 0.045 s, heavy 0.085 s, kill 0.11 s, guard-break/parry 0.14 s. Never stack (take max), clamp 0.16 s total. **Must check `GameState == Playing` before restoring timeScale** or it will unpause the pause menu; pause must cancel a running hit-stop.
   - **Trauma camera shake** on `CameraFollow`: AddTrauma 0.20 light / 0.35 heavy / 0.25 kill / 0.45 player hurt / 0.55 guard break. `shake = trauma² × Perlin`, max offset 0.25 m, max roll 1.2°, freq 25 Hz, decay 1.5/s. Expose a shake-intensity setting later (mobile motion-sickness complaint).
   - **Hit flash**: cache renderers at `AttachVisual` return; `MaterialPropertyBlock` white tint 0.09 s (probe `_Color` vs `_BaseColor` per material at cache time — glTFast Built-in materials vary; never `renderer.material`, it leaks instances).
   - **Impulse knockback**: replace one-frame `cc.Move` teleports with v₀ = knockback × 6 m/s decayed by `exp(-8·dt)`, integrated per frame. Heavy adds 2 m/s upward pop.
3. **Damage numbers**: pool of 24 world-anchored uGUI texts; rise + scale-pop 1.3→1.0 + fade; white light / orange heavy / yellow finisher; kills show the points `ScoreManager.AddKill` already returns.
4. **Hit sparks**: ONE pooled code-built `ParticleSystem`, radial `Emit(10)` biased along knockback, 0.15-0.3 s life, ember→white, additive; grey 6-particle variant for blocked hits.
5. **Camera**: look-ahead `FacingSign × 1.2 m` smoothed 0.4 s; kill punch-in dolly Z −11→−9.5 over 0.12 s, return 0.4 s; player must stay in the central 40% of screen; shake+zoom combined never exceeds 0.35 m.

---

## 4. Phase 1 — Real skeletal animation

### 4.1 Generation pipeline (Higgsfield, NSS workspace)

**Do NOT regenerate characters from images.** Run one **`3d_rigging`** job per clip against the **same base GLB URL** (the existing Hero/Grunt/Skeleton/Brute meshes), same `height_meters` every time → same auto-rig skeleton → clips bind by node path on the one instantiated character. One clip per job; no multi-clip parameter exists.

- Preflight every batch with `get_cost:true`. (Full image_to_3d+rig+anim was ~38 cr; rigging-only should be less. Budget worst case ~29 jobs × 38 ≈ 1,100 cr vs 1,705 available.)
- **Test clip sharing first:** generate ONE clip (e.g. Hit_Reaction 178) against two different characters and diff node paths. Meshy auto-rig is the same biped — if paths match, hit/death/idle clips are generated **once** and shared across all four characters, cutting jobs to ~20.
- Prefer **`_inplace` variants** (ids ~600-696) for locomotion — root motion fights the code-driven `CharacterController`. Clips without an `_inplace` twin get root X/Z zeroed offline (see 4.2).

### 4.2 Clip shortlist (from the 154-clip Fighting group scan)

**Hero** (previews: `https://cdn.meshy.ai/webapp-assets/feature-demo/animation/preview/biped/<Name>.gif`):

| State | Clip | id |
|---|---|---|
| Idle (combat) | Combat_Stance | 89 |
| Walk fwd / back | Walk_Fight_Forward / Walk_Fight_Back | 21 / 20 |
| Run | RunFast | 16 |
| Light 1 (jab) | Left_Jab_from_Guard | 191 |
| Light 2 (hook) | Left_Hook_from_Guard | 193 |
| Light 3 (uppercut/launcher) | Right_Uppercut_from_Guard | 194 |
| Sword / heavy finisher | Sword_Judgment (big) or Right_Hand_Sword_Slash (fast) | 102 / 219 |
| Block | Block1 | 138 |
| Parry (Phase 2 polish) | Sword_Parry | 147 |
| Dodge roll | Roll_Dodge | 158 |
| Hit react | Hit_Reaction (alt Face_Punch_Reaction) | 178 / 174 |
| Knockdown | Knock_Down | 187 |
| Launch (juggle) | BeHit_FlyUp (**inplace 608**) | 7 / 608 |
| Death | Dead (alt dying_backwards) | 8 / 189 |
| Victory / taunt | Victory_Fist_Pump / Chest_Pound_Taunt | 403 / 88 |

The three `_from_Guard` boxing clips (191/193/194) all start and end in the same guard pose — they chain cleanly as a state machine. Single-clip alternative: Punch_Combo family 198/200/201/203/204/205.

**Grunt:** idle 89 (shared), chase ForwardLeft/Right_Run_Fight **inplace 630/631**, attack Counterstrike 90 (alt Flying_Fist_Kick 94), hit Slap_Reaction 173, death Knock_Down_1 190.
**Skeleton:** idle Axe_Stance 85, walk Monster_Walk 112, attack Left_Slash 97 (alt Thrust_Slash 240), hit Electrocution_Reaction 172 (full-body rattle — perfect for bones), death Fall_Dead_from_Abdominal_Injury 188.
**Brute:** walk Slow_Orc_Walk 119, charge Male_Head_Down_Charge 512 (Phase 2), attack Heavy_Hammer_Swing 128 (alts Charged_Axe_Chop 237, Axe_Spin_Attack 238), hit Hit_Reaction_to_Waist 171, death Electrocuted_Fall 181 (alt 186).

Library quirks: search is substring-only ("death" → 0 results, browse category *Dying*; "sword attack" fails, use "sword"). Blocks are Block1-10 = ids 138-146 (no Block7).

### 4.3 Offline sanitize step (`tools/strip_anim_glb.mjs`, @gltf-transform/cli)

Per generated GLB: (1) prune images/textures/materials — drops ~7 MB → ~0.3 MB, keeps nodes+skin+animation; (2) zero root/hips X-Z translation for non-`_inplace` locomotion; (3) **diff node paths against the base GLB and FAIL the pipeline on mismatch**; (4) assert exactly one animation. Output: `StreamingAssets/Models/Anim/<Char>_<Action>.glb` (e.g. `Hero_Punch1.glb`, `Shared_HitReact.glb`).

### 4.4 Runtime architecture (legacy Animation + clip harvesting)

glTFast at runtime produces **Legacy** `AnimationClip`s only (Mecanim exists only via editor import), and legacy clips can't drive Animator/Playables. **Do not attempt `AvatarBuilder` runtime retargeting — dead end** (curve re-binding is editor-only). Legacy `Animation` + `CrossFade` is also the cheapest evaluator on Android (15 actors × ~40 bones ≪ 1 ms).

- **`AnimLibrary.cs`** (new): `GltfImport.Load(uri)` → `GetAnimationClips()[0]` → `Object.Instantiate(clip)`, set `legacy=true`, name, `WrapMode` (Loop: idle/walk/block, Once: attacks/hit, ClampForever: death) → **`gltf.Dispose()`** so the anim GLB's redundant mesh/textures never stay resident. Cache one clip per file — **never clone per enemy**. Sequential preload (peak memory = one GLB), failures just leave the key missing.
- **`ActorAnimator.cs`** (plain class, per actor): `TryBind` finds the glTF scene root, adds/gets `Animation`, `playAutomatically=false` + `Stop()` (the instantiator may autoplay baked Idle), `AddClip`s the cached clones, then runs a **binding probe** per clip (sample at t=0.3, assert ≥1 bone moved — legacy clips fail *silently* on path mismatch) and drops failures. API: `CrossFade(key, fade)`, `ScrubAttack(...)`, `SetFacing(sign)`.
- **Phase-locked attack scrubbing — the sync rule:** `CombatSystem`/`EnemyBase` timers stay authoritative; clips conform. Per-clip normalized windows in GameConfig (`ActiveStartNorm`/`ActiveEndNorm`, measured once with the debug cycler): Startup maps to [0, ActiveStartNorm], Active to [ActiveStartNorm, ActiveEndNorm], Recovery to the rest — `state.speed=0`, write `normalizedTime` from `Phase`+`PhaseT01` each frame. The visual contact frame lands exactly at Active start **by construction**; hitboxes and balance untouched.
- **Player seam** (`PlayerController.cs`): rename `AnimateVisual` body → `AnimateProcedural` (unchanged fallback); new `AnimateVisual`: `if (_skeletal?.Ready) TickSkeletal(); else AnimateProcedural();`. `TickSkeletal` maps the existing priority chain: dead → Die (ClampForever holds corpse); hit → Hit crossfade 0.03; attacking → ScrubAttack; blocking → Block loop; stunned → hold Hit last frame; moving → Walk 0.15; idle 0.2. Knockback/flinch offsets stay as an additive juice layer on the Visual root. **CombatSystem: zero changes.**
- **Enemy seam** (`EnemyBase.cs`): same Ready early-out; add `ClipDef[] ClipSet` to `EnemyStats`; Die clip speed-scaled to finish before the `Destroy(gameObject, 1.4f)`; WindUp+Cooldown scrub one attack clip so contact lands when `DoStrike()` fires.
- **Bootstrap**: preload all clip sets as a non-awaited task during the menu; the game plays procedurally until each `ActorAnimator` flips Ready (seamless mid-run upgrade). Add a `Debug.isDebugBuild`-only **clip cycler** (next/play/scrub slider, prints length + normalizedTime) — the tool for measuring contact frames and sanity-checking each generated clip.
- **Skinned-mesh hygiene** (`GlbLoader.cs`): after `NormalizeAndGround`, expand `SkinnedMeshRenderer.localBounds` (~2× height) or set `updateWhenOffscreen=true`, else animated poses get frustum-culled mid-swing; `Stop()` before measuring bounds.

### 4.5 Phase 1 validation checklist

1. Offline: node-path diff passes for every anim GLB; exactly 1 clip/file; hips X/Z range < 1 cm on locomotion.
2. Runtime binding probe: 0 dropped clips on all four characters.
3. Timing: visual sword contact vs CombatSystem Active entry within 1 frame at 60 fps.
4. Transition QA: idle↔walk rapid toggle, punch spam through buffer, block during walk, hit-during-attack, guard break, death in every state, restart after death.
5. Fallback QA: delete `Anim/` folder → game plays identically to today; delete one clip → only that state degrades.
6. Device: Animation.Update + skinning < 2 ms with 14 enemies + hero; texture memory unchanged after preload (proves Dispose worked); APK growth ~10-15 MB.
7. No T-pose flash on enemy spawn (enemies fight before visuals attach — first CrossFade must target the *current* state, not Idle, the frame TryBind completes).

---

## 5. Phase 2 — Combat interactivity

1. **Input buffer + cancel windows** (CombatSystem only): accept input in ANY phase, buffer 0.35 s, consume latest press at earliest legal cancel point. Recovery cancels: into next chain attack at 30% if the hit LANDED / 60% on whiff (rewards hit-confirms); into dodge at 25%; into block at 50%. Startup/Active never cancel (commitment). Drop buffer on hurt/knockdown/guard-break.
2. **3-hit light chain + heavy finisher**: replace bool-heavy with an `AttackDef` table (startup/active/recovery/dmg/reach/kb/stamina/maxTargets/hitType/clipKey). Jab 0.10/0.08/0.26 s dmg 6 → Hook 0.12/0.08/0.30 dmg 8 → Uppercut 0.16/0.10/0.42 dmg 14 *Launch*. SWORD pressed in L1/L2 cancel window = finisher: 0.15/0.14/0.45, dmg 30, cleave 3, *Knockdown*. Chain resets 0.7 s after recovery. Refund 2 stamina per landed hit (aggression economy).
3. **Dodge roll**: 6th touch button. 0.42 s, 2.6 m ease-out, i-frames 0.06-0.30 s, 12 stamina, roll-cancel into attack at 80%.
4. **Hit-reaction richness**: extend `DamageInfo` with `HitType {Light, Heavy, Launch, Knockdown}` + `StunSeconds`. Knockdown 0.9 s (one OTG hit at 50% dmg) + 0.35 s getup with invuln; player hurt becomes real 0.25 s hitstun that interrupts Startup (fixes "flinch is only visual"). Launch = juggle-lite on Grunt/Skeleton only; Brutes never launch (hyper-armor identity). Route ALL state entry through one `SetActionState()` with a legality matrix + hard timeouts.
5. **Enemy telegraphs**: 2 ember flashes at 6 Hz in last 0.25 s of WindUp; Skeleton windup 0.35→0.45 s (0.35 unreadable on phone; keep cadence so DPS unchanged); windup-inhale SFX.
6. **Attack tokens** (`AttackDirector`, ~80 lines): MaxTokens=2 (3 from wave 10), Brute costs 2; only token-holders may WindUp; ≥0.4 s stagger between windup starts; tokenless enemies ring at range+1.2 m with sinusoidal shuffle and occasional side-crossing. This single system makes 14 enemies read as fair instead of a blender.
7. **Polish tier**: parry (block pressed ≤0.15 s before impact → negate, attacker staggers 1.2 s, hit-stop 0.14 s), Brute head-down charge (clip 512, blockable, self-stagger 1.5 s on block), Grunt 20% feint, Skeleton lunge-step.

---

## 6. Phase 3 — Sound & presentation

1. **The game is silent — fill it** (Higgsfield `generate_audio`, single-digit credits each): the 16 defined SFX keys (whooshes ×3, flesh impacts ×3, sword hit, block raise/impact, guard-break shatter, hurt ×2, enemy deaths ×3, wave horn, combo chime, UI tick) + 3 stone footsteps. Pitch-randomize 0.9-1.1 at play. Whoosh at Startup→Active edge; impact on hit-confirm.
2. **`AnimEventRelay`**: legacy clips have no authored events — code table `clipName → [(normalizedTime, eventId)]` polled against `AnimationState.normalizedTime` (footsteps at 0.25/0.75 of walk, whoosh at contact-frame of attack clips).
3. **Music**: 2-layer loop (explore/combat intensity) via generate_audio, crossfaded by enemies-in-range count.
4. **Style meter rework** (`ScoreManager`): score fighting, not just kills — +1/hit, +3/kill, +2/parry; 2.5 s decay refreshed on any landed hit; D/C/B/A/S tiers at 5/12/22/35 onto existing x1-x5; getting hit drops one tier (not full reset). HUD scale-pop on tier-up + thin decay bar.
5. **HUD/death polish**: health-bar damage-lag ghost, low-HP vignette pulse, death sequence (1.2 s at timeScale 0.25, dolly to the falling hero, GameOver fade 0.4 s), VO rate-limit 4 s.
6. **Polish tier**: weapon TrailRenderer on the auto-rig hand bone (name-search "Hand"+"R"; keep a procedural slash-arc quad as fallback — bone naming may vary between generations), Android rumble via `VibrationEffect` (no-op <API 26), elites from wave 8 (+60% HP, ember rim tint), wave modifiers every 5th wave announced by VO.
7. **Perf guardrails**: pool enemies (currently new GameObject + async GLB per spawn), pool all VFX/numbers, cap flashes 6/frame and trauma adds 1/frame, drop enemy cap to 10 on <4 GB devices.

---

## 7. Top risks (full list in workflow archive)

1. **Silent clip-binding failure** — legacy clips bind by node-path string; mismatch = clip "plays", nothing moves. Mitigated three ways: same-base-GLB rigging jobs, offline path diff (fails packaging), runtime binding probe (drops clip → procedural fallback). A bad generation can never break the build.
2. **APK/memory bloat** — each 1-clip GLB carries the full ~7 MB mesh+textures. Offline strip → ~0.3 MB; clone-then-Dispose keeps only curves resident.
3. **Clip-length vs hitbox desync** — phase-locked scrubbing makes gameplay timers authoritative by construction. If time-warp exceeds ~2.5× and looks rushed, pick shorter clips (jab set) rather than touching balance.
4. **Hit-stop vs pause collision** — timeScale restore must check game state; pause must cancel hit-stop.
5. **Scope creep** — the CORE cut-line (Phases 0-2 + Phase 3 items 1-2) alone transforms the game; every POLISH item is individually skippable. Ship CORE to a device build before green-lighting POLISH.

---

## 8. Immediate next actions

1. Implement Phase 0 (no assets, no credits).
2. Preflight `3d_rigging` cost with `get_cost:true`; run the 2-character clip-sharing test (1-2 jobs).
3. Submit the hero clip batch (~13 jobs), then enemies (~12, or fewer if sharing works) while Phase 0/1 code lands.
4. Generate the SFX batch (~20 one-shots).
5. Build `AnimLibrary`/`ActorAnimator` + seams, measure contact frames with the debug cycler, run the validation checklist, device-test the APK.
