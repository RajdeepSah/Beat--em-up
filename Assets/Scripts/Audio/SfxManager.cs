using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Plays short sound effects by key from Resources/Audio/SFX. The repo ships with NO sfx
    /// (Higgsfield audio is voice-only) - drop royalty-free clips named per the keys below into
    /// Assets/Resources/Audio/SFX and they play automatically. Missing clips simply no-op, so
    /// the game runs fine silent-SFX out of the box. See Assets/Resources/Audio/SFX/README.txt.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxManager : MonoBehaviour
    {
        // Canonical SFX keys (filenames without extension). Match the brief, section 12.
        public const string PunchWhiff = "sfx_punch_whiff";
        public const string PunchHit = "sfx_punch_hit";
        public const string SwordSwing = "sfx_sword_swing";
        public const string SwordHit = "sfx_sword_hit";
        public const string BlockRaise = "sfx_block_raise";
        public const string BlockImpact = "sfx_block_impact";
        public const string GuardBreak = "sfx_guard_break";
        public const string PlayerHurt = "sfx_player_hurt";
        public const string PlayerDie = "sfx_player_die";
        public const string WaveStart = "sfx_wave_start";
        public const string WaveClear = "sfx_wave_clear";
        public const string UiButton = "sfx_ui_button";
        public const string ComboUp = "sfx_combo_up";
        public const string EnemyDieGrunt = "sfx_orc_die";
        public const string EnemyDieSkeleton = "sfx_skeleton_die";
        public const string EnemyDieBrute = "sfx_brute_die";
        public const string Dodge = "sfx_dodge";
        public const string Footstep = "sfx_footstep";
        public const string EnemyWindup = "sfx_enemy_windup";
        public const string Parry = "sfx_parry";

        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void Play(string key, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!_cache.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>(GameConfig.SfxFolder + key); // null if not supplied
                _cache[key] = clip;
            }
            if (clip != null) _source.PlayOneShot(clip, volume);
        }
    }
}
