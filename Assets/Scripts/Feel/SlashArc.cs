using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// One pooled procedural slash-arc: a camera-facing ring-segment mesh built in code,
    /// flashed for the sword's active window and faded out additively. Reads better in a
    /// side-scroller than a hand-bone TrailRenderer and has zero dependency on rig naming.
    /// Created by Bootstrap; CombatSystem shows it when a heavy attack goes active.
    /// </summary>
    public class SlashArc : MonoBehaviour
    {
        public static SlashArc Instance { get; private set; }

        private const float FadeTime = 0.16f;

        private MeshRenderer _renderer;
        private Material _material;
        private float _timer;
        private float _facing = 1f;

        private void Awake()
        {
            Instance = this;
            var filter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildArcMesh(0.85f, 1.75f, -30f, 80f, 14);

            Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            _material = new Material(sh);
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.enabled = false;
        }

        /// <summary>A vertical ring segment in the XY plane (faces the side-on camera).</summary>
        private static Mesh BuildArcMesh(float r0, float r1, float fromDeg, float toDeg, int segments)
        {
            var verts = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float a = Mathf.Deg2Rad * Mathf.Lerp(fromDeg, toDeg, t);
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts[i * 2] = dir * r0;
                verts[i * 2 + 1] = dir * r1;
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
                if (i < segments)
                {
                    int v = i * 2, q = i * 6;
                    tris[q] = v; tris[q + 1] = v + 2; tris[q + 2] = v + 1;
                    tris[q + 3] = v + 1; tris[q + 4] = v + 2; tris[q + 5] = v + 3;
                }
            }
            var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Flash the arc at the attacker's chest, sweeping in the facing direction.</summary>
        public static void Show(Vector3 origin, float facingSign)
        {
            var self = Instance;
            if (self == null) return;
            self._facing = facingSign >= 0f ? 1f : -1f;
            self.transform.position = origin + new Vector3(self._facing * 0.35f, 0f, -0.05f);
            self.transform.localScale = new Vector3(self._facing, 1f, 1f);
            self._timer = FadeTime;
            self._renderer.enabled = true;
        }

        private void Update()
        {
            if (!_renderer.enabled) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _renderer.enabled = false;
                return;
            }
            float k = _timer / FadeTime;
            Color c = GameConfig.EmberOrange;
            c.a = 0.8f * k;
            if (_material.HasProperty("_TintColor")) _material.SetColor("_TintColor", c);
            else _material.color = c;
            // slight forward sweep while fading
            transform.position += new Vector3(_facing * 2.2f * Time.deltaTime, 0f, 0f);
        }
    }
}
