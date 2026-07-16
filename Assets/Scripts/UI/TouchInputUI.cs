using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Builds the on-screen controls (section 4): a movement cluster bottom-left (LEFT/RIGHT, holds)
    /// and an action cluster bottom-right (PUNCH/SWORD taps, BLOCK hold, ROLL tap), each wired to a
    /// named PlayerController method. Uses glyph sprites with a text fallback, and shows an ember
    /// "held" ring on the hold-type buttons (LEFT/RIGHT/BLOCK) so holds are discoverable. Built under
    /// a safe-area root by HUDController so the buttons clear notches / the Android gesture bar.
    /// Combat buttons deliberately carry NO UI click SFX/haptic — combat already gives its own feedback.
    /// </summary>
    public class TouchInputUI : MonoBehaviour
    {
        public void Build(PlayerController player, Sprite buttonSprite)
        {
            Vector2 bl = new Vector2(0f, 0f);   // bottom-left anchor (movement)
            Vector2 br = new Vector2(1f, 0f);   // bottom-right anchor (actions)
            const float move = 168f;            // larger move targets (thumb rests bottom-left)
            const float act = 150f;

            // Movement — held, ember highlight, chevron glyph (one arrow art mirrored for LEFT).
            var left = UIIcons.RoundButton("BtnLeft", transform, UISprites.GlyphArrow, "<", 64,
                bl, new Vector2(150f, 165f), move, buttonSprite, Color.white, glyphScale: 0.46f, flipX: true, highlight: true);
            left.OnDown = player.OnMoveLeftDown; left.OnUp = player.OnMoveLeftUp;

            var right = UIIcons.RoundButton("BtnRight", transform, UISprites.GlyphArrow, ">", 64,
                bl, new Vector2(345f, 165f), move, buttonSprite, Color.white, glyphScale: 0.46f, highlight: true);
            right.OnDown = player.OnMoveRightDown; right.OnUp = player.OnMoveRightUp;

            // Actions — PUNCH nearest the corner (primary), SWORD above it, BLOCK (hold) + ROLL to the left.
            var punch = UIIcons.RoundButton("BtnPunch", transform, UISprites.GlyphPunch, "PUNCH", 28,
                br, new Vector2(-150f, 135f), act, buttonSprite, Color.white, glyphScale: 0.5f);
            punch.OnDown = player.OnPunch;

            var sword = UIIcons.RoundButton("BtnSword", transform, UISprites.GlyphSword, "SWORD", 28,
                br, new Vector2(-150f, 305f), act, buttonSprite, Color.white, glyphScale: 0.5f);
            sword.OnDown = player.OnSword;

            var block = UIIcons.RoundButton("BtnBlock", transform, UISprites.GlyphBlock, "BLOCK", 28,
                br, new Vector2(-322f, 212f), act, buttonSprite, Color.white, glyphScale: 0.5f, highlight: true);
            block.OnDown = player.OnBlockDown; block.OnUp = player.OnBlockUp;

            var roll = UIIcons.RoundButton("BtnRoll", transform, UISprites.GlyphRoll, "ROLL", 30,
                br, new Vector2(-322f, 62f), act, buttonSprite, Color.white, glyphScale: 0.5f);
            roll.OnDown = player.OnDodge;
        }
    }
}
