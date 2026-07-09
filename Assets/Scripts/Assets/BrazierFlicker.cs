using UnityEngine;

namespace Ironhold
{
    /// <summary>Flickers a brazier's point light with smoothed noise for a warm torch feel.</summary>
    public class BrazierFlicker : MonoBehaviour
    {
        public Light Target;
        public float BaseIntensity = 2.2f;
        public float Amplitude = 0.6f;
        public float Speed = 7f;

        private float _seed;

        private void Awake()
        {
            if (Target == null) Target = GetComponent<Light>();
            _seed = Mathf.Repeat(transform.position.x * 13.17f, 100f);
        }

        private void Update()
        {
            if (Target == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * Speed);
            Target.intensity = BaseIntensity + (n - 0.5f) * 2f * Amplitude;
        }
    }
}
