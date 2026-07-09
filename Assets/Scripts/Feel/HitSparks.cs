using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// One pooled, code-built ParticleSystem for every hit spark in the game (never one per
    /// actor). Radial ember burst biased along the knockback direction; grey small variant
    /// for blocked hits. Additive unlit material created at runtime — no editor assets.
    /// </summary>
    public class HitSparks : MonoBehaviour
    {
        public static HitSparks Instance { get; private set; }

        private ParticleSystem _ps;

        private void Awake()
        {
            Instance = this;
            _ps = gameObject.AddComponent<ParticleSystem>();

            var main = _ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;
            main.gravityModifier = 1.5f;
            main.startSpeed = 0f; // velocity supplied per-particle via EmitParams

            var emission = _ps.emission;
            emission.enabled = false; // Emit() only

            var shape = _ps.shape;
            shape.enabled = false;

            var renderer = _ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateAdditiveMaterial();
        }

        private static Material CreateAdditiveMaterial()
        {
            Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", Color.white);
            return m;
        }

        /// <summary>Ember burst at a hit point, biased along the knockback direction.</summary>
        public static void Burst(Vector3 pos, float dirSignX, bool heavy)
        {
            Instance?.EmitBurst(pos, dirSignX, heavy ? 14 : 10,
                GameConfig.EmberOrange, heavy ? 0.16f : 0.12f, 3f, 6f);
        }

        /// <summary>Small grey puff for a blocked hit.</summary>
        public static void Blocked(Vector3 pos, float dirSignX)
        {
            Instance?.EmitBurst(pos, dirSignX, 6, new Color(0.7f, 0.7f, 0.72f), 0.09f, 1.5f, 3f);
        }

        /// <summary>Big white radial flash for a successful parry.</summary>
        public static void Parry(Vector3 pos, float dirSignX)
        {
            Instance?.EmitBurst(pos, dirSignX, 20, Color.white, 0.14f, 4f, 8f);
        }

        private void EmitBurst(Vector3 pos, float dirSignX, int count, Color color, float size, float speedMin, float speedMax)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                // Radial spread biased toward the knockback side.
                Vector3 dir = Random.onUnitSphere;
                dir.z *= 0.3f;
                dir.x = Mathf.Abs(dir.x) * (dirSignX >= 0f ? 1f : -1f) * 0.8f + dir.x * 0.2f;
                if (dir.y < -0.2f) dir.y = -dir.y;

                ep.position = pos;
                ep.velocity = dir.normalized * Random.Range(speedMin, speedMax);
                ep.startColor = Color.Lerp(color, Color.white, Random.value * 0.5f);
                ep.startSize = Random.Range(size * 0.6f, size * 1.3f);
                ep.startLifetime = Random.Range(0.15f, 0.3f);
                _ps.Emit(ep, 1);
            }
        }
    }
}
