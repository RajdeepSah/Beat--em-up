IRONHOLD: ENDLESS SIEGE - Sound Effects (placeholders)
=======================================================

Higgsfield audio generation is voice-only, so the announcer voice lines in
Assets/Resources/Audio/VO/ were generated, but SFX are NOT shipped. The game runs
fine with no SFX - SfxManager simply no-ops on any missing clip.

To add sound effects, drop royalty-free .wav / .mp3 / .ogg files into THIS folder
(Assets/Resources/Audio/SFX/), named exactly as the keys below (no extension in the
name is required by Unity; "sfx_punch_hit.wav" loads as key "sfx_punch_hit").

Good free sources: freesound.org, kenney.nl (Impact/RPG packs), sonniss.com GDC packs.

Keys the game will play if present:
  sfx_punch_whiff     - light melee swing (no contact)
  sfx_punch_hit       - punch connects
  sfx_sword_swing     - heavy melee swing
  sfx_sword_hit       - sword connects
  sfx_block_raise     - raising the guard (optional, not currently triggered)
  sfx_block_impact    - a hit absorbed while blocking
  sfx_guard_break     - stamina ran out while blocking
  sfx_player_hurt     - player takes unblocked damage
  sfx_player_die      - player death
  sfx_wave_start      - a new wave begins
  sfx_wave_clear      - a wave is cleared
  sfx_ui_button       - UI button (optional)
  sfx_combo_up        - combo multiplier increased
  sfx_orc_die         - Orc Grunt death
  sfx_skeleton_die    - Skeleton death
  sfx_brute_die       - Orc Brute death

Optional ambience (loop it yourself on an AudioSource if you want it):
  amb_courtyard_night

After adding files, no code changes are needed - they are loaded by name at runtime.
