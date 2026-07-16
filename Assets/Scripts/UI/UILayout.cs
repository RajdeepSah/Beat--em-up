using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Layout helpers so controllers stop hand-signing pixel offsets. The main entry point is
    /// <see cref="SafeAreaRoot"/>: parent interactive content to it and every corner-anchored child
    /// insets to the device safe area for free. Edge/corner math is provided for the on-screen
    /// controls, which position relative to the (safe) screen edges.
    /// </summary>
    public static class UILayout
    {
        public enum Anchor
        {
            TopLeft, TopCenter, TopRight,
            CenterLeft, Center, CenterRight,
            BottomLeft, BottomCenter, BottomRight
        }

        /// <summary>A full-rect child that auto-insets to the device safe area (adds a SafeAreaFitter).
        /// Keep full-bleed art on the raw root; put text / buttons / controls under this.</summary>
        public static RectTransform SafeAreaRoot(string name, Transform parent)
        {
            var rt = UIFactory.FullScreen(name, parent);
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        /// <summary>Normalised anchor/pivot vector for an edge or corner.</summary>
        public static Vector2 Vec(Anchor a)
        {
            float x = (a == Anchor.TopLeft || a == Anchor.CenterLeft || a == Anchor.BottomLeft) ? 0f
                    : (a == Anchor.TopRight || a == Anchor.CenterRight || a == Anchor.BottomRight) ? 1f : 0.5f;
            float y = (a == Anchor.TopLeft || a == Anchor.TopCenter || a == Anchor.TopRight) ? 1f
                    : (a == Anchor.BottomLeft || a == Anchor.BottomCenter || a == Anchor.BottomRight) ? 0f : 0.5f;
            return new Vector2(x, y);
        }

        /// <summary>anchoredPosition for a margin measured inward from the chosen edge/corner
        /// (used with anchorMin=anchorMax=pivot=Vec(a)).</summary>
        public static Vector2 Inset(Anchor a, Vector2 margin)
        {
            Vector2 v = Vec(a);
            float x = v.x == 0f ? margin.x : (v.x == 1f ? -margin.x : 0f);
            float y = v.y == 0f ? margin.y : (v.y == 1f ? -margin.y : 0f);
            return new Vector2(x, y);
        }
    }
}
