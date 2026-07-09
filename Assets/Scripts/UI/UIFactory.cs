using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Builds all uGUI elements in code (no prefabs). Thin helpers over RectTransform / Image /
    /// Text / TouchButton so the UI controllers stay readable. All text uses the builtin runtime
    /// font via RenderUtil so it works on a fresh project with no TextMeshPro import.
    /// </summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A RectTransform stretched to fill its parent (overlay / menu root).</summary>
        public static RectTransform FullScreen(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
            bool raycast = false, bool sliced = false)
        {
            var rt = Rect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sliced && sprite != null) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Label(string name, Transform parent, string content, int fontSize,
            TextAnchor align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var rt = Rect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = RenderUtil.UIFont();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A labelled button using the stone panel sprite. Returns the TouchButton.</summary>
        public static TouchButton PanelButton(string name, Transform parent, string label, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
            Sprite panelSprite)
        {
            // Type.Simple: the panel sprite is created at runtime via Sprite.Create with no border,
            // so 9-slicing would just warn and render as Simple anyway.
            var img = Image(name, parent, panelSprite, panelSprite != null ? Color.white : GameConfig.Iron,
                anchorMin, anchorMax, pivot, anchoredPos, size, raycast: true, sliced: false);
            Label(name + "_Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .rectTransform.offsetMin = Vector2.zero;
            var btn = img.gameObject.AddComponent<TouchButton>();
            return btn;
        }

        /// <summary>A round control button (move / action) with a center glyph. Returns the TouchButton.</summary>
        public static TouchButton RoundButton(string name, Transform parent, string glyph, int glyphSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, float diameter,
            Sprite buttonSprite, Color tint)
        {
            var img = Image(name, parent, buttonSprite, buttonSprite != null ? tint : GameConfig.Iron,
                anchorMin, anchorMax, pivot, anchoredPos, new Vector2(diameter, diameter), raycast: true);
            var lbl = Label(name + "_Glyph", img.transform, glyph, glyphSize, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lbl.rectTransform.offsetMin = Vector2.zero;
            lbl.rectTransform.offsetMax = Vector2.zero;
            var btn = img.gameObject.AddComponent<TouchButton>();
            return btn;
        }

        /// <summary>A simple filled bar (background + coloured fill anchored left). Returns the fill Image.</summary>
        public static Image Bar(string name, Transform parent, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var bg = Image(name + "_BG", parent, null, new Color(0f, 0f, 0f, 0.6f),
                anchorMin, anchorMax, pivot, anchoredPos, size);
            // Fill is left-anchored; HUD scales it by setting fill.rectTransform.anchorMax.x = fraction.
            var fill = Image(name + "_Fill", bg.transform, null, fillColor,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return fill;
        }
    }
}
