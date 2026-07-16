using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Pause overlay: PAUSED, RESUME, RESTART, SETTINGS, QUIT TO MENU, best score. Fades via
    /// UITransition (unscaled — Pause runs at timeScale 0). Content under a safe-area root.</summary>
    public class PauseController : MonoBehaviour
    {
        private RectTransform _root;
        private UITransition _transition;
        private Text _bestText;

        public void Build(Transform canvas, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("Pause", canvas);
            _root.gameObject.AddComponent<CanvasGroup>();
            _transition = _root.gameObject.AddComponent<UITransition>();

            UIFactory.Image("PauseScrim", _root, null, UITheme.Scrim,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);

            var safe = UILayout.SafeAreaRoot("PauseSafe", _root);

            UIFactory.Label("PausedTitle", safe, "PAUSED", UITheme.H1, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 244), new Vector2(800, 100));

            float w = UITheme.MenuBtnWidth, h = 96f;
            var resume = UIFactory.PanelButton("Resume", safe, "RESUME", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 112), new Vector2(w, h), panelSprite);
            resume.OnDown = () => GameManager.Instance?.ResumeGame();

            var restart = UIFactory.PanelButton("RestartP", safe, "RESTART", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(w, h), panelSprite);
            restart.OnDown = () => GameManager.Instance?.Restart();

            var settingsBtn = UIFactory.PanelButton("PauseSettings", safe, "SETTINGS", 40,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(w, h), panelSprite);
            settingsBtn.OnDown = () => SettingsController.Instance?.Open();

            var quit = UIFactory.PanelButton("QuitP", safe, "QUIT TO MENU", 36,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -206), new Vector2(w, h), panelSprite);
            quit.OnDown = () => GameManager.Instance?.QuitToMenu();

            _bestText = UIFactory.Label("PauseBest", safe, "BEST: 0", 28, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(600, 40));

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            _transition.SetShownImmediate((gm != null ? gm.State : GameState.Menu) == GameState.Paused);
        }

        private void OnStateChanged(GameState s)
        {
            bool paused = s == GameState.Paused;
            if (_transition != null) { if (paused) _transition.Show(); else _transition.Hide(); }
            if (paused)
            {
                var score = GameManager.Instance?.Score;
                if (_bestText != null && score != null) _bestText.text = "BEST: " + score.Best.ToString("N0");
            }
        }
    }
}
