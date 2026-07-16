using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Pipeline-agnostic helpers. Every material is created at runtime and works under BOTH
    /// the Built-in render pipeline and URP (URP shader first, Built-in fallback), so the
    /// project renders correctly whichever pipeline the user ends up on. Also centralises
    /// runtime font and sprite loading.
    /// </summary>
    public static class RenderUtil
    {
        private static Font s_UIFont;

        public static Material CreateLit(Color color, Texture texture = null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = sh != null;
            if (!urp) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Diffuse"); // last-ditch legacy

            var m = new Material(sh);
            if (urp)
            {
                m.SetColor("_BaseColor", color);
                if (texture != null) m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Smoothness", 0.1f);
            }
            else
            {
                if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                if (texture != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
            }
            return m;
        }

        public static Material CreateUnlit(Color color, Texture texture = null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            bool urp = sh != null;
            if (!urp) sh = Shader.Find("Unlit/Texture");
            if (sh == null) sh = Shader.Find("Sprites/Default");

            var m = new Material(sh);
            if (urp)
            {
                m.SetColor("_BaseColor", color);
                if (texture != null) m.SetTexture("_BaseMap", texture);
            }
            else
            {
                if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                if (texture != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            }
            return m;
        }

        public static Font UIFont()
        {
            if (s_UIFont != null) return s_UIFont;
            // Unity 6 / 2022+ ships this builtin dynamic font.
            s_UIFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (s_UIFont == null) s_UIFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (s_UIFont == null) s_UIFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 16);
            return s_UIFont;
        }

        public static Texture2D LoadTexture(string resourcePath)
        {
            return Resources.Load<Texture2D>(resourcePath);
        }

        /// <summary>Load a texture from Resources and wrap it in a Sprite for uGUI (import-setting independent).</summary>
        public static Sprite LoadSprite(string resourcePath)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Load a texture as a 9-sliceable Sprite so framed panels keep a constant border at any size
        /// instead of stretching. <paramref name="borderPx"/> is the frame inset in source pixels;
        /// <paramref name="ppu"/> (pixels-per-unit) tunes rendered border thickness — higher = thinner.
        /// Consume with Image.type = Sliced (see UIFactory.PanelButton).
        /// </summary>
        public static Sprite LoadSpriteSliced(string resourcePath, float borderPx, float ppu)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            float b = Mathf.Min(borderPx, Mathf.Min(tex.width, tex.height) * 0.5f - 1f);
            var border = new Vector4(b, b, b, b);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                ppu, 0, SpriteMeshType.FullRect, border);
        }
    }
}
