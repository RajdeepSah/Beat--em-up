using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Central runtime UI sprite registry, populated once by Bootstrap and read by the UI controllers,
    /// so a dozen sprites don't have to thread through every Build(...) signature. Any field may be null
    /// if the art isn't present yet — every consumer must handle null (icons render transparent via
    /// UIFactory.Icon; icon-buttons fall back to a text glyph).
    /// </summary>
    public static class UISprites
    {
        // Core
        public static Sprite Button;   // Button_Round (transparent round face, tintable)
        public static Sprite Panel;    // Panel_Stone (9-sliced forged frame)
        public static Sprite Title;    // TitleImage_16x9

        // Control glyphs
        public static Sprite GlyphArrow, GlyphPunch, GlyphSword, GlyphBlock, GlyphRoll, GlyphSettings;

        // HUD status icons
        public static Sprite IconHealth, IconStamina, IconCoin, IconWave;

        // Style-tier badges (index D=0, C=1, B=2, A=3, S=4 — matches GameConfig.StyleTierNames)
        public static readonly Sprite[] TierBadge = new Sprite[5];

        // Framed status-bar backing
        public static Sprite FrameBar;

        public static void LoadAll()
        {
            Button = RenderUtil.LoadSprite(GameConfig.TexButton);
            Panel  = RenderUtil.LoadSpriteSliced(GameConfig.TexPanel, UITheme.PanelBorderPx, UITheme.PanelSpritePPU);
            Title  = RenderUtil.LoadSprite(GameConfig.TexTitle);

            GlyphArrow    = RenderUtil.LoadSprite(GameConfig.GlyphArrow);
            GlyphPunch    = RenderUtil.LoadSprite(GameConfig.GlyphPunch);
            GlyphSword    = RenderUtil.LoadSprite(GameConfig.GlyphSword);
            GlyphBlock    = RenderUtil.LoadSprite(GameConfig.GlyphBlock);
            GlyphRoll     = RenderUtil.LoadSprite(GameConfig.GlyphRoll);
            GlyphSettings = RenderUtil.LoadSprite(GameConfig.GlyphSettings);

            IconHealth  = RenderUtil.LoadSprite(GameConfig.IconHealth);
            IconStamina = RenderUtil.LoadSprite(GameConfig.IconStamina);
            IconCoin    = RenderUtil.LoadSprite(GameConfig.IconCoin);
            IconWave    = RenderUtil.LoadSprite(GameConfig.IconWave);

            for (int i = 0; i < GameConfig.TierBadgePaths.Length && i < TierBadge.Length; i++)
                TierBadge[i] = RenderUtil.LoadSprite(GameConfig.TierBadgePaths[i]);

            FrameBar = RenderUtil.LoadSpriteSliced(GameConfig.FrameBar, GameConfig.FrameBarBorderPx, 100f);
        }
    }
}
