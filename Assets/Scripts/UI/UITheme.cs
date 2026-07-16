using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Central UI design tokens: colors, a type scale, spacing, tap-target minimums, outline/shadow,
    /// 9-slice sizing and press-feel. References the locked <see cref="GameConfig"/> palette so color
    /// stays single-source. Code-first in the same spirit as GameConfig — tune the whole UI look here
    /// rather than hunting hardcoded numbers across the controllers.
    ///
    /// NOTE (legacy Text, not TMP): the project ships no TMP font asset / TMP Essentials, so a TMP
    /// migration would render with a missing font and force a full re-layout. We stay on
    /// UnityEngine.UI.Text and buy legibility with Outline/Shadow (see UIFactory.Label). If TMP is ever
    /// imported, build a dynamic SDF via TMP_FontAsset.CreateFontAsset(RenderUtil.UIFont()).
    /// </summary>
    public static class UITheme
    {
        // ---- Colors (semantic; resolve to the locked palette) ----
        public static readonly Color TextPrimary  = GameConfig.Bone;
        public static readonly Color TextMuted     = new Color(0.847f, 0.812f, 0.753f, 0.62f);
        public static readonly Color Accent        = GameConfig.EmberOrange;
        public static readonly Color Danger        = new Color(0.78f, 0.22f, 0.20f);          // HP fill
        public static readonly Color OutlineColor  = new Color(0f, 0f, 0f, 0.78f);
        public static readonly Color ShadowColor   = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Scrim         = new Color(0.045f, 0.055f, 0.082f, 0.86f); // night-blue veil
        public static readonly Color BarTrack      = new Color(0f, 0f, 0f, 0.62f);
        public static readonly Color HighlightRing = new Color(0.910f, 0.454f, 0.231f, 0.95f); // ember (held state)

        // ---- Type scale (px at the 1920x1080 reference canvas) ----
        public const int Title    = 120;
        public const int H1       = 68;
        public const int H2       = 50;
        public const int BtnLarge = 46;
        public const int Btn      = 36;
        public const int Body     = 32;
        public const int Label    = 26;
        public const int Small    = 20;

        // ---- Outline / shadow offsets ----
        public static readonly Vector2 OutlineDistance = new Vector2(2f, -2f);
        public static readonly Vector2 ShadowDistance  = new Vector2(3f, -3f);

        // ---- Spacing / tap targets ----
        public const float GutterEdge    = 48f;
        public const float StackGap      = 22f;
        public const float TapTargetMin  = 120f;   // min touch diameter/height (accessibility)
        public const float MenuBtnWidth  = 480f;
        public const float MenuBtnHeight = 128f;

        // ---- 9-slice for Panel_Stone ----
        // rendered border (UI px) = PanelBorderPx * (canvasRefPPU 100 / PanelSpritePPU).
        // 150 * 100/400 ≈ 38px — 38*2 = 76 fits inside even the shortest (84px) button without
        // corner overlap. QA against the final panel art; raise PPU if corners blow out.
        public const float PanelBorderPx  = 150f;
        public const float PanelSpritePPU = 400f;

        // ---- Press feel (shared by TouchButton) ----
        public const float PressScale    = 0.92f;
        public const float IdleAlpha     = 0.82f;
        public const float ActiveAlpha   = 1f;
        public const float DisabledAlpha = 0.4f;
    }
}
