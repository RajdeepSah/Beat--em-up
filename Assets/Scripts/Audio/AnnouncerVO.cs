using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// The herald announcer. Plays the Higgsfield TTS lines (Resources/Audio/VO) on game events.
    /// One voice at a time (a new line interrupts the previous one).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AnnouncerVO : MonoBehaviour
    {
        public enum Line
        {
            RunStart, Wave2, WaveBrute, WaveMilestone,
            ComboX3, ComboX5, GuardBreak, LowHP, WaveCleared, GameOver
        }

        private static readonly Dictionary<Line, string> s_Files = new Dictionary<Line, string>
        {
            { Line.RunStart, "vo_run_start" },
            { Line.Wave2, "vo_wave_2" },
            { Line.WaveBrute, "vo_wave_brute" },
            { Line.WaveMilestone, "vo_wave_milestone" },
            { Line.ComboX3, "vo_combo_x3" },
            { Line.ComboX5, "vo_combo_x5" },
            { Line.GuardBreak, "vo_guard_break" },
            { Line.LowHP, "vo_low_hp" },
            { Line.WaveCleared, "vo_wave_cleared" },
            { Line.GameOver, "vo_game_over" },
        };

        private readonly Dictionary<Line, AudioClip> _clips = new Dictionary<Line, AudioClip>();
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D
            _source.volume = 0.9f;
            foreach (var kv in s_Files)
            {
                var clip = Resources.Load<AudioClip>(GameConfig.VoFolder + kv.Value);
                if (clip != null) _clips[kv.Key] = clip;
            }
        }

        public void Play(Line line)
        {
            if (!_clips.TryGetValue(line, out var clip) || clip == null) return;
            _source.Stop();
            _source.clip = clip;
            _source.Play();
        }
    }
}
