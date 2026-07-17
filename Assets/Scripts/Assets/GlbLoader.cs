using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Loads the Higgsfield-generated GLB meshes from StreamingAssets/Models via glTFast at
    /// runtime, caches each import so it can be instantiated many times cheaply, normalises
    /// every model to a consistent height and grounds it at the actor's feet. If glTFast or a
    /// file is unavailable it spawns a tinted capsule placeholder, so the game ALWAYS runs and
    /// is always playable - it never hard-fails on a missing/odd asset.
    /// </summary>
    public static class GlbLoader
    {
        private static readonly Dictionary<string, GltfImport> s_Cache = new Dictionary<string, GltfImport>();
        private static readonly HashSet<string> s_Failed = new HashSet<string>();

        // Cached build-safe shader (resolved once). glTFast picks its own shader at runtime and
        // that shader gets stripped from a player build (Android especially), which is why models
        // render magenta/pink on device but look fine in the editor. We re-point every loaded
        // material at THIS shader instead - one that is guaranteed to be in the build because it
        // is also listed under Project Settings > Graphics > Always Included Shaders.
        private static Shader s_SafeShader;

        private static Shader SafeShader()
        {
            if (s_SafeShader != null) return s_SafeShader;
            // Try in order of preference; the first one that is actually included in the build wins.
            s_SafeShader = Shader.Find("Universal Render Pipeline/Lit");   // if the project ever moves to URP
            if (s_SafeShader == null) s_SafeShader = Shader.Find("Standard");            // Built-in pipeline (this project)
            if (s_SafeShader == null) s_SafeShader = Shader.Find("Legacy Shaders/Diffuse");
            if (s_SafeShader == null) s_SafeShader = Shader.Find("Sprites/Default");     // last-resort, always present
            return s_SafeShader;
        }

        private static string StreamingUri(string modelName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Models", modelName + ".glb");
            path = path.Replace("\\", "/");
            // Android streamingAssetsPath already has a jar:file:// scheme; desktop/editor needs file://.
            if (!path.Contains("://")) path = "file://" + path;
            return path;
        }

        /// <summary>Load a model's GLB into the cache. Safe to call repeatedly. Returns false on failure.</summary>
        public static async Task<bool> Preload(string modelName)
        {
            if (s_Cache.ContainsKey(modelName)) return true;
            if (s_Failed.Contains(modelName)) return false;
            try
            {
                var gltf = new GltfImport();
                bool ok = await gltf.Load(StreamingUri(modelName));
                if (ok)
                {
                    s_Cache[modelName] = gltf;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GlbLoader] Failed to load '{modelName}': {e.Message}. Using placeholder.");
            }
            s_Failed.Add(modelName);
            return false;
        }

        public static async Task PreloadAll(IEnumerable<string> modelNames)
        {
            foreach (var n in modelNames) await Preload(n);
        }

        /// <summary>
        /// Create a "Visual" child under <paramref name="parent"/>, populate it with the model
        /// (or a placeholder), normalise to targetHeight and ground it at the parent origin.
        /// Returns the Visual root transform so callers can animate it procedurally.
        /// </summary>
        public static async Task<Transform> AttachVisual(string modelName, Transform parent, float targetHeight, Color fallbackColor)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);

            bool built = false;
            if (await Preload(modelName) && s_Cache.TryGetValue(modelName, out var gltf))
            {
                try
                {
                    built = await gltf.InstantiateMainSceneAsync(visual.transform);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GlbLoader] Instantiate '{modelName}' failed: {e.Message}. Using placeholder.");
                    built = false;
                }
            }

            // The actor/prop may have been destroyed during the awaits above (e.g. WaveManager
            // cleared the wave on restart/game-over). Bail before touching destroyed transforms.
            if (parent == null || visual == null)
            {
                if (visual != null) UnityEngine.Object.Destroy(visual);
                return null;
            }

            if (!built)
            {
                BuildPlaceholder(visual.transform, fallbackColor);
            }
            else
            {
                // FIX: swap glTFast's runtime shaders for a build-safe one so models are not pink on device.
                ForceBuildSafeShaders(visual.transform);
            }

            NormalizeAndGround(visual.transform, parent, targetHeight);
            StripColliders(visual.transform); // gameplay collision lives on the actor root, not the art
            ExpandSkinnedBounds(visual.transform); // animated poses must never get frustum-culled mid-swing
            return visual.transform;
        }

        /// <summary>
        /// glTFast assigns its own shaders when it instantiates a model; those shaders are stripped
        /// from a player build, so on device the model draws with the fallback error shader (bright
        /// magenta/pink) even though it looks correct in the editor. This walks every renderer on the
        /// freshly loaded model and re-points each material at a shader that IS in the build
        /// (see SafeShader), carrying the base colour texture and tint across so the model still looks
        /// right. Editor rendering is unchanged; the phone stops showing pink.
        /// </summary>
        private static void ForceBuildSafeShaders(Transform root)
        {
            Shader safe = SafeShader();
            if (safe == null) return; // nothing we can do; leave glTFast's material as-is

            foreach (var rend in root.GetComponentsInChildren<Renderer>())
            {
                // .materials gives us editable per-renderer instance copies (not the shared asset).
                var mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (m.shader == safe) continue; // already safe

                    // 1) Capture the base colour texture glTFast assigned, checking the property
                    //    names glTFast/URP/Built-in use, before we change the shader.
                    Texture baseTex = m.mainTexture;
                    if (baseTex == null && m.HasProperty("_BaseColorTexture")) baseTex = m.GetTexture("_BaseColorTexture");
                    if (baseTex == null && m.HasProperty("_BaseMap"))          baseTex = m.GetTexture("_BaseMap");
                    if (baseTex == null && m.HasProperty("baseColorTexture"))  baseTex = m.GetTexture("baseColorTexture");

                    // 2) Capture the base colour / tint.
                    Color baseColor = Color.white;
                    if (m.HasProperty("_BaseColor"))          baseColor = m.GetColor("_BaseColor");
                    else if (m.HasProperty("baseColorFactor")) baseColor = m.GetColor("baseColorFactor");
                    else if (m.HasProperty("_Color"))          baseColor = m.color;

                    // 2b) Capture glTFast's PBR factors BEFORE the swap (Standard has no such
                    //     properties, so they are lost otherwise). glTFast surfaces are matte by
                    //     default - glTF roughnessFactor defaults to 1, i.e. smoothness 0 - but the
                    //     Standard shader defaults to 0.5 smoothness, which adds a glossy ambient/
                    //     skybox reflection that greys the albedo out. That is the "washed out /
                    //     whitish" look on device. Carry the real values across so the Standard
                    //     surface responds exactly as glTFast did in the editor.
                    float metallic = 0f;
                    if (m.HasProperty("metallicFactor"))      metallic = m.GetFloat("metallicFactor");
                    else if (m.HasProperty("_Metallic"))      metallic = m.GetFloat("_Metallic");
                    float smoothness = 0f; // roughnessFactor 1 -> smoothness 0 (matte, like the editor)
                    if (m.HasProperty("roughnessFactor"))     smoothness = 1f - m.GetFloat("roughnessFactor");
                    else if (m.HasProperty("_Glossiness"))    smoothness = m.GetFloat("_Glossiness");

                    // 3) Swap to the build-safe shader.
                    m.shader = safe;

                    // 4) Re-apply what we captured onto the standard property slots.
                    if (baseTex != null)
                    {
                        m.mainTexture = baseTex;
                        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", baseTex);
                    }
                    if (m.HasProperty("_Color"))     m.color = baseColor;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);

                    // 5) Reproduce glTFast's matte PBR response so the base colour reads at full
                    //    saturation instead of being washed out by Standard's default gloss.
                    if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", metallic);
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
                }
                rend.materials = mats;
            }
        }

        private static void BuildPlaceholder(Transform parent, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Placeholder";
            go.transform.SetParent(parent, false);
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = RenderUtil.CreateLit(color);
        }

        private static void NormalizeAndGround(Transform visual, Transform parent, float targetHeight)
        {
            if (!TryGetWorldBounds(visual, out Bounds b)) return;
            if (b.size.y > 0.0001f)
            {
                float scale = targetHeight / b.size.y;
                visual.localScale *= scale;
            }
            // Recompute after scaling, then shift so feet sit at the parent origin, centred on X
            // and keeping the parent's authored Z (props live behind the lane; actors are at PlayZ).
            if (!TryGetWorldBounds(visual, out b)) return;
            Vector3 target = new Vector3(parent.position.x, parent.position.y, parent.position.z);
            Vector3 current = new Vector3(b.center.x, b.min.y, b.center.z);
            visual.position += (target - current);
        }

        private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                bounds = new Bounds(root.position, Vector3.zero);
                return false;
            }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static void StripColliders(Transform root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.Destroy(c);
        }

        /// <summary>
        /// Skinned meshes keep their bind-pose local bounds; a swinging or lying pose can
        /// leave them and get frustum-culled mid-animation. Give every SkinnedMeshRenderer
        /// generous bounds instead (cheaper than updateWhenOffscreen on mobile).
        /// </summary>
        private static void ExpandSkinnedBounds(Transform root)
        {
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Bounds b = smr.localBounds;
                float pad = Mathf.Max(b.size.x, b.size.y, b.size.z);
                b.Expand(pad * 1.5f);
                smr.localBounds = b;
            }
        }
    }
}