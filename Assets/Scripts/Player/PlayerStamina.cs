using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Player stamina (section 6). Spending an action delays regen; continuous drain (block)
    /// keeps regen paused while held. Regenerates after StaminaRegenDelay seconds of no spend.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Max => GameConfig.PlayerMaxStamina;
        public float Normalized => Mathf.Clamp01(Current / Max);

        private float _lastSpendTime;

        public void ResetActor()
        {
            Current = Max;
            _lastSpendTime = -10f;
        }

        public bool CanSpend(float amount) => Current >= amount;

        public void Spend(float amount)
        {
            Current = Mathf.Max(0f, Current - amount);
            _lastSpendTime = Time.time;
        }

        /// <summary>Give stamina back (landed-hit refund). Does not delay regen.</summary>
        public void Refund(float amount)
        {
            Current = Mathf.Min(Max, Current + amount);
        }

        /// <summary>Drain over time (used while blocking). Returns true if stamina just hit 0.</summary>
        public bool DrainContinuous(float perSecond)
        {
            bool wasPositive = Current > 0f;
            Current = Mathf.Max(0f, Current - perSecond * Time.deltaTime);
            _lastSpendTime = Time.time;
            return wasPositive && Current <= 0f;
        }

        private void Update()
        {
            if (Time.time - _lastSpendTime >= GameConfig.StaminaRegenDelay && Current < Max)
                Current = Mathf.Min(Max, Current + GameConfig.StaminaRegenPerSec * Time.deltaTime);
        }
    }
}
