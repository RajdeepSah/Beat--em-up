using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Two-layer adaptive music from Resources/Audio/Music: a menu drone, a combat drum
    /// base, and an intensity layer that fades in with the size of the crowd. Base and
    /// intensity start sample-locked on the same dspTime so they never drift. Missing
    /// files simply no-op (same philosophy as SfxManager). Volumes duck under SFX.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        private const float MenuVolume = 0.30f;
        private const float CombatVolume = 0.34f;
        private const float IntenseMax = 0.30f;
        private const float FadeSpeed = 1.2f; // volume units / second

        /// <summary>Music-bus level (0..1), driven by the Settings screen. Static so SettingsService can
        /// set it without a reference (this manager is created anonymously by Bootstrap).</summary>
        public static float Volume01 = 1f;

        private AudioSource _menu, _base, _intense;
        private bool _started;

        private void Awake()
        {
            _menu = MakeSource("music_menu");
            _base = MakeSource("music_combat_base");
            _intense = MakeSource("music_combat_intense");
        }

        private AudioSource MakeSource(string clipName)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = Resources.Load<AudioClip>(GameConfig.MusicFolder + clipName); // null-safe
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.ignoreListenerPause = true;
            return src;
        }

        private void Start()
        {
            // Everything runs at volume 0 until faded in; sample-lock the combat layers.
            double t0 = AudioSettings.dspTime + 0.1;
            if (_menu.clip != null) _menu.Play();
            if (_base.clip != null) _base.PlayScheduled(t0);
            if (_intense.clip != null) _intense.PlayScheduled(t0);
            _started = true;
        }

        private void Update()
        {
            if (!_started) return;
            var gm = GameManager.Instance;
            bool inCombat = gm != null && (gm.State == GameState.Playing || gm.State == GameState.Paused);

            float crowd = 0f;
            if (gm != null && gm.State == GameState.Playing && gm.Waves != null)
                crowd = Mathf.Clamp01((gm.Waves.AliveCount - 2) / 6f);

            float dt = Time.unscaledDeltaTime * FadeSpeed;
            _menu.volume = Mathf.MoveTowards(_menu.volume, (inCombat ? 0f : MenuVolume) * Volume01, dt);
            _base.volume = Mathf.MoveTowards(_base.volume, (inCombat ? CombatVolume : 0f) * Volume01, dt);
            _intense.volume = Mathf.MoveTowards(_intense.volume, (inCombat ? IntenseMax * crowd : 0f) * Volume01, dt);
        }
    }
}
