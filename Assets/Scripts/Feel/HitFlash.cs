using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Full-silhouette hit flash (Streets-of-Rage style): briefly swaps every renderer under
    /// the actor's visual to a shared unlit flash material, then restores the originals.
    /// Swapping shared materials is pipeline-proof (no _Color/_BaseColor probing) and leaks
    /// nothing (two static flash materials serve every actor in the scene). Also drives the
    /// enemy wind-up telegraph pulses. Attach to the actor root; call Init after the GLB lands.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        private static Material s_White, s_Ember;

        private Renderer[] _renderers;
        private Material[][] _original;
        private float _timer;
        private bool _swapped;

        public bool IsInitialized => _renderers != null && _renderers.Length > 0;

        public void Init(Transform visualRoot)
        {
            RestoreNow(); // in case a flash was live when the visual got replaced
            if (visualRoot == null) { _renderers = null; return; }
            _renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            _original = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _original[i] = _renderers[i].sharedMaterials;
        }

        public void FlashWhite(float duration = GameConfig.HitFlashTime) => Flash(WhiteMat(), duration);
        public void FlashEmber(float duration = GameConfig.HitFlashTime) => Flash(EmberMat(), duration);

        /// <summary>One short ember pulse (wind-up telegraph). Safe to call repeatedly.</summary>
        public void TelegraphPulse() => Flash(EmberMat(), 0.07f);

        private void Flash(Material mat, float duration)
        {
            if (!IsInitialized) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var mats = new Material[_original[i].Length];
                for (int m = 0; m < mats.Length; m++) mats[m] = mat;
                r.sharedMaterials = mats;
            }
            _swapped = true;
            _timer = Mathf.Max(_timer, duration);
        }

        private void Update()
        {
            if (!_swapped) return;
            _timer -= Time.unscaledDeltaTime; // flashes read through hit-stop
            if (_timer <= 0f) RestoreNow();
        }

        private void RestoreNow()
        {
            if (_swapped && _renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null || _original[i] == null) continue;
                    _renderers[i].sharedMaterials = _original[i];
                }
            }
            _swapped = false;
            _timer = 0f;
        }

        private void OnDestroy() => RestoreNow();

        private static Material WhiteMat()
        {
            if (s_White == null) s_White = RenderUtil.CreateUnlit(Color.white);
            return s_White;
        }

        private static Material EmberMat()
        {
            if (s_Ember == null) s_Ember = RenderUtil.CreateUnlit(GameConfig.EmberOrange);
            return s_Ember;
        }
    }
}
