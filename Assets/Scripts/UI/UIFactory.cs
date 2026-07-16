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
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
            bool outline = true, bool shadow = false)
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
            // Outline/shadow buy legibility over the busy 3D scene. The effect colour is fixed, so it
            // won't track a text alpha-fade — pass outline:false on alpha-animated labels (banner,
            // combo, floating damage numbers).
            if (shadow)
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = UITheme.ShadowColor;
                sh.effectDistance = UITheme.ShadowDistance;
            }
            if (outline)
            {
                var ol = t.gameObject.AddComponent<Outline>();
                ol.effectColor = UITheme.OutlineColor;
                ol.effectDistance = UITheme.OutlineDistance;
            }
            return t;
        }

        /// <summary>A labelled button using the stone panel sprite. Returns the TouchButton.</summary>
        public static TouchButton PanelButton(string name, Transform parent, string label, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
            Sprite panelSprite, bool uiFeedback = true)
        {
            // Sliced: the panel is loaded via RenderUtil.LoadSpriteSliced with a 9-slice border, so the
            // forged iron frame keeps a constant thickness instead of distorting on wide/short buttons.
            var img = Image(name, parent, panelSprite, panelSprite != null ? Color.white : GameConfig.Iron,
                anchorMin, anchorMax, pivot, anchoredPos, size, raycast: true, sliced: panelSprite != null);
            Label(name + "_Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .rectTransform.offsetMin = Vector2.zero;
            var btn = img.gameObject.AddComponent<TouchButton>();
            btn.UseUiFeedback = uiFeedback;   // menu-family buttons click + rumble (combat controls opt out)
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

        /// <summary>A non-interactive icon image. If the sprite is missing it renders fully transparent
        /// (never a coloured box), so the UI degrades gracefully before art is dropped in.</summary>
        public static Image Icon(string name, Transform parent, Sprite sprite, Color tint,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            Color c = sprite != null ? tint : new Color(tint.r, tint.g, tint.b, 0f);
            var img = Image(name, parent, sprite, c, anchorMin, anchorMax, pivot, anchoredPos, size, raycast: false);
            img.preserveAspect = true;
            return img;
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

        /// <summary>A "label + slider" settings row (0..1), top-anchored at anchoredPos. Returns the Slider.
        /// Built from runtime Images; the track raycasts so drags reach the Slider.</summary>
        public static Slider SliderRow(string name, Transform parent, string label, float value,
            Vector2 anchoredPos, float width, System.Action<float> onChanged)
        {
            var row = Rect(name + "Row", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                anchoredPos, new Vector2(width, 56f));
            Label(name + "Lbl", row, label, UITheme.Label, TextAnchor.MiddleLeft, UITheme.TextPrimary,
                new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), new Vector2(6, 0), Vector2.zero);

            var s = Rect(name, row, new Vector2(0.44f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            s.offsetMin = new Vector2(0f, -9f); s.offsetMax = new Vector2(0f, 9f);
            var slider = s.gameObject.AddComponent<Slider>();

            var track = Image(name + "Track", s, null, UITheme.BarTrack, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);
            track.rectTransform.offsetMin = Vector2.zero; track.rectTransform.offsetMax = Vector2.zero;
            var fill = Image(name + "Fill", s, null, UITheme.Accent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: false);
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            var handle = Image(name + "Handle", s, null, UITheme.TextPrimary, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 34f), raycast: true);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        /// <summary>A "label + on/off pill" settings row. The pip slides right + turns ember when on.
        /// Returns the TouchButton (UI feedback on).</summary>
        public static TouchButton ToggleRow(string name, Transform parent, string label, bool value,
            Vector2 anchoredPos, float width, System.Action<bool> onChanged)
        {
            var row = Rect(name + "Row", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                anchoredPos, new Vector2(width, 56f));
            Label(name + "Lbl", row, label, UITheme.Label, TextAnchor.MiddleLeft, UITheme.TextPrimary,
                new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0.5f), new Vector2(6, 0), Vector2.zero);

            const float pillW = 92f, pillH = 40f;
            var box = Image(name + "Box", row, null, UITheme.BarTrack, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                Vector2.zero, new Vector2(pillW, pillH), raycast: true);
            var pip = Image(name + "Pip", box.transform, null, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(pillH - 8f, pillH - 8f), raycast: false);
            var btn = box.gameObject.AddComponent<TouchButton>();
            btn.UseUiFeedback = true;

            bool[] st = { value };
            System.Action refresh = () =>
            {
                pip.color = st[0] ? UITheme.Accent : UITheme.TextMuted;
                pip.rectTransform.anchoredPosition = new Vector2(st[0] ? pillW - pillH * 0.5f : pillH * 0.5f, 0f);
            };
            refresh();
            btn.OnDown = () => { st[0] = !st[0]; refresh(); onChanged?.Invoke(st[0]); };
            return btn;
        }
    }
}
