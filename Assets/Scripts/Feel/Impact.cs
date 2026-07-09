using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// The impact kernel: hit-stop (brief Time.timeScale dips on unscaled time) plus the
    /// static facade every combat event calls for feedback (trauma shake, kill punch-in).
    /// Stops never stack (take max of remaining vs new, clamped to HitstopMax) and always
    /// respect pause: the pause menu cancels any running stop, and time is only restored
    /// to 1 while actually Playing. Added to the Systems object by Bootstrap.
    /// </summary>
    public class Impact : MonoBehaviour
    {
        public static Impact Instance { get; private set; }

        private float _stopRemaining;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_stopRemaining <= 0f) return;
            _stopRemaining -= Time.unscaledDeltaTime;
            if (_stopRemaining <= 0f) Restore();
        }

        private void Restore()
        {
            _stopRemaining = 0f;
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Playing) Time.timeScale = 1f;
        }

        /// <summary>Freeze time briefly. Non-stacking; no-op outside of Playing.</summary>
        public static void HitStop(float seconds)
        {
            var self = Instance;
            var gm = GameManager.Instance;
            if (self == null || gm == null || gm.State != GameState.Playing) return;
            self._stopRemaining = Mathf.Min(Mathf.Max(self._stopRemaining, seconds), GameConfig.HitstopMax);
            Time.timeScale = GameConfig.HitstopScale;
        }

        /// <summary>Pause is taking over Time.timeScale — abandon any running stop.</summary>
        public static void CancelHitStop()
        {
            if (Instance != null) Instance._stopRemaining = 0f;
        }

        public static void Trauma(float amount)
        {
            GameManager.Instance?.Camera?.AddTrauma(amount);
        }

        // ---- Combat event presets ----

        public static void PlayerHitLanded(bool heavy)
        {
            HitStop(heavy ? GameConfig.HitstopHeavy : GameConfig.HitstopLight);
            Trauma(heavy ? GameConfig.TraumaHeavyHit : GameConfig.TraumaLightHit);
            if (heavy) Rumble.Heavy(); else Rumble.Light();
        }

        public static void EnemyKilled(bool heavy)
        {
            HitStop(GameConfig.HitstopKill);
            Trauma(GameConfig.TraumaKill);
            GameManager.Instance?.Camera?.KillPunch(heavy ? 1f : 0.55f);
        }

        public static void PlayerHurt()
        {
            Trauma(GameConfig.TraumaPlayerHurt);
            Rumble.Hurt();
        }

        public static void GuardBreakFeedback()
        {
            HitStop(GameConfig.HitstopGuardBreak);
            Trauma(GameConfig.TraumaGuardBreak);
            Rumble.GuardBreak();
        }

        public static void ParryFeedback()
        {
            HitStop(GameConfig.HitstopGuardBreak);
            Trauma(0.3f);
            Rumble.Heavy();
        }
    }
}
