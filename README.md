# IRONHOLD: ENDLESS SIEGE

A single-player 3D side-scrolling beat-'em-up for Android, built in Unity. A lone armored
knight holds the torchlit courtyard of a doomed keep against endless, escalating waves of
orcs and skeletons. Punch, sword, and block your way to a high score.

This is a complete, local Unity project. All art (characters, props, textures, UI, app icon)
and the announcer voice lines were generated with Higgsfield and are bundled here. You open
the project in Unity and build the APK on your own machine - nothing is hosted or deployed.

---

## What's in the box

```
IronholdEndlessSiege/
  Assets/
    Scripts/            All C# (Core, Player, Enemies, Combat, Camera, UI, Audio, Scoring, Assets)
    Resources/
      Art/Textures/     Floor, wall, night-sky textures
      Art/UI/           Button + panel sprites, Menu/ title image
      Art/Icon/         AppIcon_1024.png (set this as the Android app icon - see below)
      Audio/VO/         10 announcer voice lines (generated)
      Audio/SFX/        (empty) drop royalty-free SFX here - see that folder's README
    StreamingAssets/
      Models/           7 rigged/textured GLB meshes (Hero, Grunt, Skeleton, Brute, Crate, Barrel, Brazier)
    Art/RefImages/      The 2D concept images the 3D models were generated from (reference only)
  Packages/manifest.json
  README.md
```

There are intentionally **no scenes, prefabs, or ProjectSettings checked in**. The game builds
itself in code at runtime (see "How it works"), so there is nothing fragile to merge or break.

---

## Requirements

- **Unity 6 LTS** (6000.0.x). Unity 2022.3 LTS also works.
- The **glTFast** package (`com.unity.cloud.gltfast`) - already listed in `Packages/manifest.json`,
  so Unity installs it automatically on first open. It is what loads the `.glb` meshes at runtime.
- Android Build Support module (with OpenJDK, Android SDK & NDK) - install it via Unity Hub:
  Installs > your Unity version > Add Modules > Android Build Support.

---

## Open and play (editor)

1. **Unity Hub > Add > Add project from disk** and pick this `IronholdEndlessSiege` folder.
   Open it with Unity 6 LTS. First open takes a minute while packages resolve.
2. If the Console shows a glTFast resolve error, open **Window > Package Manager**, click **+ >
   Add package by name**, enter `com.unity.cloud.gltfast`, and let it install the version it
   recommends. (Then you can remove the explicit version from `manifest.json` if you like.)
3. **Make a boot scene** (the project ships none on purpose):
   **File > New Scene > Empty (or Basic)**, then **File > Save As** -> `Assets/Scenes/Boot.unity`.
   The scene can be completely empty - the game bootstraps itself.
4. Open **File > Build Settings**, click **Add Open Scenes** so `Boot` is in the list (index 0).
5. Press **Play**. The main menu builds itself; press **PLAY** to start a run.

> Why an empty scene works: `Bootstrap.cs` runs automatically via
> `[RuntimeInitializeOnLoadMethod]` and constructs the camera, lighting, arena, player, enemies
> and the entire UI in code. It also clears any leftover default Camera/Light, so even the
> default sample scene is fine.

---

## Build the Android APK

On your machine, in Unity:

1. **File > Build Settings > Android > Switch Platform.**
2. **Player Settings** (Edit > Project Settings > Player), Android tab:
   - **Resolution and Presentation:** Default Orientation = **Landscape Left** (or Auto Rotation
     with only the two landscape orientations enabled). The game also forces landscape at runtime.
   - **Other Settings:**
     - Scripting Backend: **IL2CPP** for a release build (Mono is fine for fast local testing).
     - Target Architectures: **ARM64** (untick ARMv7).
     - Minimum API Level: **Android 8.0 (API 26)**. Target API Level: Automatic (highest installed).
     - (Optional) Graphics APIs: Vulkan then OpenGLES3.
   - **Icon:** set the default icon to `Assets/Resources/Art/Icon/AppIcon_1024.png`
     (drag it into the Default Icon slot; assign adaptive/round icons too if you want).
   - **Product Name:** `Ironhold Endless Siege`  |  **Package Name:** `com.nyalia.ironholdendlesssiege`.
3. Connect an Android device with **USB debugging** enabled (or use an emulator).
4. **Build And Run** (produces and installs an APK), or **Build** to get the `.apk` to sideload.
   For a Play Store upload, switch the Build to **App Bundle (AAB)**.

Unity cannot be run in the environment that generated this project, so the APK is built here,
by you. Everything needed to do that is in this folder.

---

## Controls (touch)

- **Bottom-left:** `<` and `>` move the knight left/right (camera is side-locked and follows on X).
- **Bottom-right:** **PUNCH** (fast, light), **SWORD** (slow, heavy, cleaves up to 3), **BLOCK**
  (hold to cut incoming damage 80% from the front - drains stamina; empty stamina = guard break).
- Top-right **II** pauses.

In the editor you can also click the on-screen buttons with the mouse.

---

## How it works (for editing)

- **`Assets/Scripts/Core/GameConfig.cs`** is the single tuning file: HP, damage, reach, stamina,
  wave formulas, scoring tiers, palette, and all resource paths. Balance the whole game from here.
- **`Bootstrap.cs`** builds the scene in code and wires the systems through `GameManager`.
- Combat is one shared path: `IDamageable` + `DamageInfo` + `MeleeHitbox` (a `Physics.OverlapBox`
  filtered by component, so **no physics layers need to be set up**).
- Enemies are one class (`EnemyBase`) driven by data (`EnemyStats`); waves come from `WaveManager`.

### Animation

Characters now play **real skeletal clips**: one-clip GLBs (generated with Higgsfield
`3d_rigging` against the shipped base models, so the skeletons match exactly) live in
`StreamingAssets/Models/Anim/`, stripped to hierarchy + curves (~40-100 KB each) by
`tools/strip_anim_glb.py`. At runtime `AnimLibrary` harvests each file's legacy clip via
glTFast and `ActorAnimator` plays them with crossfades; attack clips are **scrubbed** so the
visual contact frame always lands exactly when the hitbox fires (gameplay timings in
`GameConfig` stay authoritative). The clip registry (keys, files, wrap modes, contact
windows) is `GameConfig.HeroClips` / `GruntClips` / `SkeletonClips` / `BruteClips`.

The original **procedural** animation is still in the code as an automatic fallback: delete
the `Anim/` folder and the game plays exactly like before — a missing or bad clip can never
break the build. In development builds an on-screen **clip cycler** (F9 in editor, 4-finger
tap on device) scrubs any clip to measure contact frames.

### Audio

- **Announcer voice** (10 lines) is generated and in `Resources/Audio/VO/` - it plays automatically.
- **SFX** are procedurally synthesized WAVs (whooshes, impacts, clanks, death sounds, UI)
  created by `tools/gen_sfx.py` into `Assets/Resources/Audio/SFX/` using the `SfxManager` key
  names. Rerun the script to regenerate, or replace any file with a royalty-free clip of the
  same name.
- **Music** is a synthesized adaptive loop set from `tools/gen_music.py` in
  `Assets/Resources/Audio/Music/`: a menu drone plus a siege-drum combat base with an
  intensity layer that `MusicManager` fades in as the crowd grows. Replace the three WAVs
  with licensed tracks of the same names for a production soundtrack.

### Render pipeline

All runtime materials are pipeline-agnostic (URP shader with a Built-in fallback), so the project
renders correctly on **either** the Built-in pipeline (default, no setup) **or** URP. For URP,
create a URP asset and assign it under Project Settings > Graphics; otherwise Built-in just works.

---

## Troubleshooting

- **Pink/magenta materials:** the active render pipeline lacks the expected shaders. The code
  falls back automatically, but if you switched to URP, make sure a URP asset is assigned.
- **Characters are capsules:** a `.glb` failed to load (glTFast missing, or StreamingAssets not
  readable). The placeholder capsule means the game still runs - check the Console and confirm
  glTFast installed.
- **A model faces the wrong way:** flip `FacingYOffset` (set it to 180) on `PlayerController` /
  `EnemyBase` - it's a cosmetic rotation only.
- **Buttons don't respond:** ensure there is an EventSystem (Bootstrap creates one) and that the
  project is using the legacy Input Manager (this project does not include the new Input System).

Hold the line, knight.
