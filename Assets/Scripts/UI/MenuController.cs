using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Main menu: title art, PLAY, HOW TO PLAY, SETTINGS and the best score. Full-bleed art
    /// on the root; text/buttons under a safe-area root. Fades in/out via UITransition.</summary>
    public class MenuController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _howTo;
        private UITransition _transition;
        private Text _bestText;

        public void Build(Transform canvas, Sprite titleSprite, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("MainMenu", canvas);
            _root.gameObject.AddComponent<CanvasGroup>();
            _transition = _root.gameObject.AddComponent<UITransition>();

            UIFactory.Image("Bg", _root, titleSprite, titleSprite != null ? Color.white : GameConfig.NightBlue,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Image("Scrim", _root, null, new Color(0f, 0f, 0f, 0.4f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var safe = UILayout.SafeAreaRoot("MenuSafe", _root);

            UIFactory.Label("Title", safe, "IRONHOLD", UITheme.Title, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1200, 140), shadow: true);
            UIFactory.Label("Subtitle", safe, "ENDLESS SIEGE", 60, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -262), new Vector2(1200, 80));
            UIFactory.Label("Tagline", safe, "How long can you hold the line?", 30, TextAnchor.MiddleCenter, UITheme.TextMuted,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -332), new Vector2(1000, 50));

            var play = UIFactory.PanelButton("Play", safe, "PLAY", UITheme.BtnLarge,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(UITheme.MenuBtnWidth, UITheme.MenuBtnHeight), panelSprite);
            play.OnDown = () => GameManager.Instance?.StartRun();

            var howToBtn = UIFactory.PanelButton("HowToBtn", safe, "HOW TO PLAY", UITheme.Btn,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -116), new Vector2(UITheme.MenuBtnWidth, 92), panelSprite);
            howToBtn.OnDown = () => { if (_howTo != null) _howTo.gameObject.SetActive(true); };

            var settingsBtn = UIFactory.PanelButton("SettingsBtn", safe, "SETTINGS", UITheme.Btn,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -224), new Vector2(UITheme.MenuBtnWidth, 92), panelSprite);
            settingsBtn.OnDown = () => SettingsController.Instance?.Open();

            _bestText = UIFactory.Label("Best", safe, "BEST: 0", 30, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(600, 50));

            BuildHowTo(panelSprite);

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            _transition.SetShownImmediate((gm != null ? gm.State : GameState.Menu) == GameState.Menu);
        }

        private void BuildHowTo(Sprite panelSprite)
        {
            _howTo = UIFactory.FullScreen("HowTo", _root);
            UIFactory.Image("HowToScrim", _howTo, null, new Color(0f, 0f, 0f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);
            var safe = UILayout.SafeAreaRoot("HowToSafe", _howTo);
            UIFactory.Label("HowToTitle", safe, "HOW TO PLAY", UITheme.H2, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(900, 80));
            UIFactory.Label("HowToBody", safe,
                "MOVE with the left / right arrows.\nPUNCH for fast combos.\nSWORD for heavy, armour-breaking hits.\nHold BLOCK to guard — time it to PARRY.\nROLL to dodge through attacks.\n\nSurvive the waves. There is no end — only your score.",
                32, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(1100, 470), outline: false);
            var back = UIFactory.PanelButton("HowToBack", safe, "BACK", UITheme.Btn,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(320, 92), panelSprite);
            back.OnDown = () => _howTo.gameObject.SetActive(false);
            _howTo.gameObject.SetActive(false);
        }

        private void OnStateChanged(GameState s)
        {
            bool menu = s == GameState.Menu;
            if (_transition != null) { if (menu) _transition.Show(); else _transition.Hide(); }
            if (menu)
            {
                if (_howTo != null) _howTo.gameObject.SetActive(false);
                var score = GameManager.Instance?.Score;
                if (_bestText != null && score != null) _bestText.text = "BEST: " + score.Best.ToString("N0");
            }
        }
    }
}
