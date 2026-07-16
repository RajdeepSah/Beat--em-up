using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Builders for icon-driven controls that consume the generated glyph sprites. Falls back to a text
    /// glyph when a sprite is missing, so controls still work before the art is dropped in.
    /// </summary>
    public static class UIIcons
    {
        /// <summary>
        /// A round control button showing a centred glyph sprite (text fallback if null). Can mirror the
        /// glyph horizontally (one arrow serves both directions) and add an ember "held" highlight ring
        /// wired to the TouchButton so holds are visually obvious. Returns the TouchButton.
        /// </summary>
        public static TouchButton RoundButton(string name, Transform parent, Sprite glyph, string fallbackGlyph,
            int fallbackSize, Vector2 anchor, Vector2 anchoredPos, float diameter, Sprite buttonSprite, Color tint,
            float glyphScale = 0.5f, bool flipX = false, bool highlight = false, bool uiFeedback = false)
        {
            var img = UIFactory.Image(name, parent, buttonSprite, buttonSprite != null ? tint : GameConfig.Iron,
                anchor, anchor, new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(diameter, diameter), raycast: true);

            Graphic ring = null;
            if (highlight)
            {
                var r = UIFactory.Image(name + "_Held", img.transform, buttonSprite, UITheme.HighlightRing,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: false);
                r.rectTransform.offsetMin = Vector2.zero; r.rectTransform.offsetMax = Vector2.zero;
                r.enabled = false;
                ring = r;
            }

            if (glyph != null)
            {
                float g = diameter * glyphScale;
                var icon = UIFactory.Icon(name + "_Glyph", img.transform, glyph, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(g, g));
                if (flipX) icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                var lbl = UIFactory.Label(name + "_Glyph", img.transform, fallbackGlyph, fallbackSize,
                    TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero, outline: false);
                lbl.rectTransform.offsetMin = Vector2.zero; lbl.rectTransform.offsetMax = Vector2.zero;
            }

            var btn = img.gameObject.AddComponent<TouchButton>();
            btn.Highlight = ring;
            btn.UseUiFeedback = uiFeedback;
            return btn;
        }
    }
}
