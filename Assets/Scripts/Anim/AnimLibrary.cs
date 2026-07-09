using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Loads Higgsfield one-clip animation GLBs from StreamingAssets/Models/Anim at runtime,
    /// harvests each file's legacy AnimationClip (glTFast produces LEGACY clips at runtime —
    /// Mecanim clips only exist via editor import), clones it, then Disposes the import so the
    /// anim GLB's redundant mesh/textures never stay resident. One cached clip per FILE, shared
    /// by every actor that plays it — never clone per enemy. All failures are soft: a missing
    /// or bad file just leaves its key unbound and the actor keeps its procedural animation.
    /// </summary>
    public static class AnimLibrary
    {
        private static readonly Dictionary<string, AnimationClip> s_Clips = new Dictionary<string, AnimationClip>();
        private static readonly HashSet<string> s_Failed = new HashSet<string>();

        public static AnimationClip Get(string file)
        {
            return file != null && s_Clips.TryGetValue(file, out var clip) ? clip : null;
        }

        private static string StreamingUri(string file)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Models",
                GameConfig.AnimSubFolder, file + ".glb");
            path = path.Replace("\\", "/");
            if (!path.Contains("://")) path = "file://" + path;
            return path;
        }

        /// <summary>Load every clip in a set. Sequential on purpose: transient memory peak = one GLB.</summary>
        public static async Task PreloadSet(GameConfig.ClipDef[] set)
        {
            foreach (var def in set)
            {
                if (def.File == null) continue; // "use the base model's baked clip"
                await LoadFile(def);
            }
        }

        private static async Task LoadFile(GameConfig.ClipDef def)
        {
            if (s_Clips.ContainsKey(def.File) || s_Failed.Contains(def.File)) return;
            try
            {
                var gltf = new GltfImport();
                bool ok = await gltf.Load(StreamingUri(def.File));
                if (ok)
                {
                    var clips = gltf.GetAnimationClips();
                    if (clips != null && clips.Length > 0 && clips[0] != null)
                    {
                        // Clone BEFORE Dispose so the curves survive the import teardown.
                        var clone = UnityEngine.Object.Instantiate(clips[0]);
                        clone.name = def.File;
                        clone.legacy = true;
                        clone.wrapMode = def.Wrap;
                        s_Clips[def.File] = clone;
                    }
                    else
                    {
                        Debug.LogWarning($"[AnimLibrary] '{def.File}.glb' contains no animation clip.");
                        s_Failed.Add(def.File);
                    }
                    gltf.Dispose();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnimLibrary] Failed to load clip '{def.File}': {e.Message}");
            }
            s_Failed.Add(def.File);
        }
    }
}
