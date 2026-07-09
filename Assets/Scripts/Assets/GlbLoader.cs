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

            if (!built) BuildPlaceholder(visual.transform, fallbackColor);

            NormalizeAndGround(visual.transform, parent, targetHeight);
            StripColliders(visual.transform); // gameplay collision lives on the actor root, not the art
            ExpandSkinnedBounds(visual.transform); // animated poses must never get frustum-culled mid-swing
            return visual.transform;
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
