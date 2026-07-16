using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Insets its RectTransform to <see cref="Screen.safeArea"/> so HUD / menu content clears notches,
    /// punch-holes and the Android gesture bar. Full-bleed art (backgrounds, vignette, centre banner)
    /// must stay OUTSIDE this rect. Recomputes only when the safe area or resolution actually changes,
    /// so the per-frame cost is a couple of comparisons. Works with the CanvasScaler because the anchors
    /// it writes are normalised (0..1).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastArea || Screen.width != _lastScreen.x || Screen.height != _lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _lastArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = _lastArea.position;
            Vector2 max = _lastArea.position + _lastArea.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;
            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
