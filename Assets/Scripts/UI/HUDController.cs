using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// In-game HUD (section 13): HP + stamina bars with status icons, WAVE / SCORE / STYLE readouts,
    /// the centre wave banner, the pause button and the on-screen controls. Full-bleed art (vignette,
    /// banner) lives on the root; everything interactive sits under a safe-area root so it clears
    /// notches / the gesture bar. Fades in/out via UITransition. Reads live values each frame.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private RectTransform _root;      // full-bleed layer (vignette + banner)
        private RectTransform _safe;      // safe-area content (bars, readouts, pause, controls)
        private RectTransform _controls;
        private Text _hpValue, _waveText, _scoreText, _comboText, _banner;
        private Image _hpFill, _staminaFill, _ghostFill, _vignette, _decayFill, _tierBadge;
        private UITransition _transition;
        private float _bannerTimer;
        private float _ghost = 1f;
        private float _comboPop;
        private int _lastMult = 1;

        public void Build(Transform canvas, PlayerController player, Sprite panelSprite, Sprite buttonSprite)
        {
            _root = UIFactory.FullScreen("HUD", canvas);
            _root.gameObject.AddComponent<CanvasGroup>();
            _transition = _root.gameObject.AddComponent<UITransition>();

            // Low-HP vignette: full-bleed, first child so everything draws over it.
            _vignette = UIFactory.Image("Vignette", _root, null, new Color(0.55f, 0.04f, 0.04f, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _vignette.rectTransform.offsetMin = Vector2.zero;
            _vignette.rectTransform.offsetMax = Vector2.zero;

            // Safe-area content root.
            _safe = UILayout.SafeAreaRoot("HUDSafe", _root);

            // HP: forged-heart icon + bar (icon replaces the old "HP" text label).
            UIFactory.Icon("HpIcon", _safe, UISprites.IconHealth, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -46), new Vector2(52, 52));
            _hpFill = UIFactory.Bar("HpBar", _safe, UITheme.Danger,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(104, -56), new Vector2(322, 30));
            _ghostFill = UIFactory.Image("HpGhost", _hpFill.transform.parent, null, new Color(1f, 0.9f, 0.8f, 0.55f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            _ghostFill.rectTransform.offsetMin = Vector2.zero;
            _ghostFill.rectTransform.offsetMax = Vector2.zero;
            _hpFill.transform.SetAsLastSibling();
            _hpValue = UIFactory.Label("HpValue", _hpFill.transform.parent, "100 / 100", 20, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _hpValue.rectTransform.offsetMin = Vector2.zero; _hpValue.rectTransform.offsetMax = Vector2.zero;

            // Stamina: flame icon + bar.
            UIFactory.Icon("StaminaIcon", _safe, UISprites.IconStamina, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -104), new Vector2(42, 42));
            _staminaFill = UIFactory.Bar("StaminaBar", _safe, GameConfig.EmberOrange,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(104, -110), new Vector2(288, 18));

            // Wave (top centre).
            _waveText = UIFactory.Label("WaveText", _safe, "WAVE 1", 32, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(300, 44));

            // Pause button (top-right corner) — larger tap target, uses a glyph fallback of "II".
            var pause = UIIcons.RoundButton("BtnPause", _safe, null, "II", 34,
                new Vector2(1, 1), new Vector2(-72, -66), 96f, buttonSprite, Color.white, uiFeedback: true);
            pause.OnDown = () => GameManager.Instance?.TogglePause();

            // Score + style (top right, left of pause).
            _scoreText = UIFactory.Label("ScoreText", _safe, "SCORE 0", 32, TextAnchor.MiddleRight, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -40), new Vector2(340, 40));
            UIFactory.Icon("CoinIcon", _safe, UISprites.IconCoin, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-486, -52), new Vector2(38, 38));
            _comboText = UIFactory.Label("ComboText", _safe, "x1", 28, TextAnchor.MiddleRight, new Color(1, 1, 1, 0.45f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -86), new Vector2(210, 36), outline: false);
            _tierBadge = UIFactory.Icon("TierBadge", _safe, null, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-372, -88), new Vector2(56, 56));
            _tierBadge.gameObject.SetActive(false);

            // Style decay bar under the combo.
            _decayFill = UIFactory.Bar("StyleDecay", _safe, GameConfig.EmberOrange,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -112), new Vector2(200, 6));

            // Centre wave banner (full-bleed, fades out).
            _banner = UIFactory.Label("Banner", _root, "", 90, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 140), outline: false);
            SetAlpha(_banner, 0f);

            // On-screen controls (under the safe-area root).
            _controls = UILayout.SafeAreaRoot("Controls", _root);
            var input = _controls.gameObject.AddComponent<TouchInputUI>();
            input.Build(player, buttonSprite);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                if (gm.Waves != null) gm.Waves.WaveStarted += ShowBanner;
            }
            GameState s0 = gm != null ? gm.State : GameState.Menu;
            _transition.SetShownImmediate(s0 == GameState.Playing || s0 == GameState.Paused);
            if (_controls != null) _controls.gameObject.SetActive(s0 == GameState.Playing);
        }

        private void OnStateChanged(GameState s)
        {
            bool inGame = s == GameState.Playing || s == GameState.Paused;
            if (_transition != null)
            {
                if (inGame) _transition.Show(12f);   // quick so combat input isn't delayed
                else _transition.Hide(12f);
            }
            if (_controls != null) _controls.gameObject.SetActive(s == GameState.Playing);
        }

        private void ShowBanner(int wave)
        {
            if (_banner == null) return;
            _banner.text = "WAVE " + wave;
            _bannerTimer = 1.8f;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || _root == null || !_root.gameObject.activeSelf) return;

            if (gm.PlayerHealth != null)
            {
                float hp = gm.PlayerHealth.Normalized;
                _hpFill.rectTransform.anchorMax = new Vector2(hp, 1f);
                _hpValue.text = $"{Mathf.CeilToInt(Mathf.Max(0, gm.PlayerHealth.HP))} / {Mathf.CeilToInt(gm.PlayerHealth.Max)}";

                _ghost = hp < _ghost ? Mathf.MoveTowards(_ghost, hp, 0.4f * Time.unscaledDeltaTime) : hp;
                _ghostFill.rectTransform.anchorMax = new Vector2(_ghost, 1f);

                bool low = gm.PlayerHealth.HP > 0f && gm.PlayerHealth.HP <= GameConfig.LowHpThreshold;
                float targetA = low ? 0.10f + 0.05f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
                Color vc = _vignette.color;
                vc.a = Mathf.MoveTowards(vc.a, targetA, Time.unscaledDeltaTime * 0.8f);
                _vignette.color = vc;
            }
            if (gm.PlayerStamina != null)
                _staminaFill.rectTransform.anchorMax = new Vector2(gm.PlayerStamina.Normalized, 1f);
            if (gm.Waves != null)
                _waveText.text = "WAVE " + gm.Waves.CurrentWave;
            if (gm.Score != null)
            {
                _scoreText.text = "SCORE " + gm.Score.Score.ToString("N0");
                int mult = gm.Score.Multiplier;
                if (mult > _lastMult) _comboPop = 1f;   // tier-up: scale pop
                _lastMult = mult;

                bool showStyle = mult > 1;
                if (showStyle)
                {
                    _comboText.text = gm.Score.TierName + "  x" + mult;
                    _comboText.color = GameConfig.EmberOrange;
                    int tier = Mathf.Clamp(gm.Score.Tier, 0, UISprites.TierBadge.Length - 1);
                    Sprite badge = UISprites.TierBadge[tier];
                    _tierBadge.sprite = badge;
                    _tierBadge.enabled = badge != null;
                    if (!_tierBadge.gameObject.activeSelf) _tierBadge.gameObject.SetActive(true);
                }
                else
                {
                    _comboText.text = "x1";
                    _comboText.color = new Color(1, 1, 1, 0.45f);
                    if (_tierBadge.gameObject.activeSelf) _tierBadge.gameObject.SetActive(false);
                }

                _comboPop = Mathf.MoveTowards(_comboPop, 0f, Time.unscaledDeltaTime / 0.15f);
                float pop = 1f + 0.4f * _comboPop;
                _comboText.rectTransform.localScale = Vector3.one * pop;
                if (_tierBadge.gameObject.activeSelf) _tierBadge.rectTransform.localScale = Vector3.one * pop;

                _decayFill.transform.parent.gameObject.SetActive(showStyle);
                if (showStyle)
                    _decayFill.rectTransform.anchorMax = new Vector2(gm.Score.DecayFraction01, 1f);
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
                SetAlpha(_banner, Mathf.Clamp01(_bannerTimer / 0.6f));
            }
        }

        private static void SetAlpha(Text t, float a)
        {
            if (t == null) return;
            Color c = t.color; c.a = a; t.color = c;
        }
    }
}
