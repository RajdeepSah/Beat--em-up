using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Score + the STYLE METER. Style rewards FIGHTING, not just kills: landed hits, kills and
    /// parries all feed the meter; tiers D/C/B/A/S map onto the x1..x5 score multipliers. The
    /// meter's decay window refreshes on any style event, then drains; getting hit drops ONE
    /// tier (blocked hits are free). Persistent best score kept from the original design.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int Best { get; private set; }
        public int ComboCount { get; private set; }   // kills inside the current style run (VO/stats)
        public bool IsNewBest { get; private set; }
        public float StyleMeter { get; private set; }

        /// <summary>0 = D .. 4 = S.</summary>
        public int Tier
        {
            get
            {
                var t = GameConfig.StyleTierThresholds;
                for (int i = t.Length - 1; i >= 0; i--)
                    if (StyleMeter >= t[i]) return i + 1;
                return 0;
            }
        }

        public string TierName => GameConfig.StyleTierNames[Tier];
        public int Multiplier => Tier + 1;

        /// <summary>How much of the decay window remains (drives the HUD drain bar).</summary>
        public float DecayFraction01 =>
            Mathf.Clamp01(1f - (Time.time - _lastStyleTime) / GameConfig.StyleDecayWindow);

        private float _lastStyleTime;

        private void Awake()
        {
            Best = PlayerPrefs.GetInt(GameConfig.BestScoreKey, 0);
        }

        public void ResetRun()
        {
            Score = 0;
            ComboCount = 0;
            StyleMeter = 0f;
            IsNewBest = false;
            _lastStyleTime = Time.time;
        }

        private void Update()
        {
            if (StyleMeter > 0f && Time.time - _lastStyleTime > GameConfig.StyleDecayWindow)
            {
                StyleMeter = Mathf.Max(0f, StyleMeter - GameConfig.StyleDrainPerSec * Time.deltaTime);
                if (StyleMeter <= 0f) ComboCount = 0;
            }
        }

        private void AddStyle(float amount)
        {
            StyleMeter += amount;
            _lastStyleTime = Time.time;
        }

        /// <summary>Landed player hits (one call per swing, scaled by targets hit).</summary>
        public void NotifyHit(int hits = 1) => AddStyle(GameConfig.StylePerHit * hits);

        public void NotifyParry() => AddStyle(GameConfig.StylePerParry);

        /// <summary>Register a kill. Style feeds first so the milestone kill earns the new tier.</summary>
        public int AddKill(int basePoints)
        {
            ComboCount++;
            AddStyle(GameConfig.StylePerKill);
            int award = basePoints * Multiplier;
            Score += award;
            return award;
        }

        public void AddWaveBonus(int waveNumber)
        {
            Score += GameConfig.WaveClearBonusPerWave * waveNumber;
        }

        /// <summary>Taking a real hit drops ONE tier (not a full reset); blocked hits never call this.</summary>
        public void NotifyPlayerHit()
        {
            int tier = Tier;
            if (tier <= 0) { StyleMeter = 0f; ComboCount = 0; return; }
            var t = GameConfig.StyleTierThresholds;
            StyleMeter = tier >= 2 ? t[tier - 2] : 0f; // to the floor of the tier below
            _lastStyleTime = Time.time;
        }

        /// <summary>Persist the best score if beaten. Returns true if a new record was set.</summary>
        public bool CommitBest()
        {
            if (Score > Best)
            {
                Best = Score;
                IsNewBest = true;
                PlayerPrefs.SetInt(GameConfig.BestScoreKey, Best);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }
    }
}
