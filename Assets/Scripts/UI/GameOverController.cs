using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Game-over screen: THE KEEP HAS FALLEN, wave reached, score, best, RESTART, MAIN MENU.
    /// Full-bleed scrim on the root; content under a safe-area root. Fades in via UITransition.</summary>
    public class GameOverController : MonoBehaviour
    {
        private RectTransform _root;
        private UITransition _transition;
        private Text _waveText, _scoreText, _bestText;

        public void Build(Transform canvas, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("GameOver", canvas);
            _root.gameObject.AddComponent<CanvasGroup>();
            _transition = _root.gameObject.AddComponent<UITransition>();

            UIFactory.Image("GoScrim", _root, null, new Color(0.04f, 0.05f, 0.08f, 0.9f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);

            var safe = UILayout.SafeAreaRoot("GameOverSafe", _root);

            UIFactory.Label("GoTitle", safe, "THE KEEP HAS FALLEN", UITheme.H1, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 100), shadow: true);

            _waveText = UIFactory.Label("GoWave", safe, "WAVE REACHED: 1", 40, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 176), new Vector2(900, 56));
            _scoreText = UIFactory.Label("GoScore", safe, "SCORE: 0", 52, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 96), new Vector2(900, 70));
            _bestText = UIFactory.Label("GoBest", safe, "BEST: 0", 36, TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 34), new Vector2(900, 50));

            float w = UITheme.MenuBtnWidth, h = 96f;
            var restart = UIFactory.PanelButton("RestartGo", safe, "RESTART", 44,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -84), new Vector2(w, h), panelSprite);
            restart.OnDown = () => GameManager.Instance?.Restart();

            var menu = UIFactory.PanelButton("MenuGo", safe, "MAIN MENU", 38,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -196), new Vector2(w, h), panelSprite);
            menu.OnDown = () => GameManager.Instance?.QuitToMenu();

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            _transition.SetShownImmediate((gm != null ? gm.State : GameState.Menu) == GameState.GameOver);
        }

        private void OnStateChanged(GameState s)
        {
            bool over = s == GameState.GameOver;
            if (_transition != null) { if (over) _transition.Show(); else _transition.Hide(); }
            if (over)
            {
                var gm = GameManager.Instance;
                if (gm?.Waves != null) _waveText.text = "WAVE REACHED: " + gm.Waves.CurrentWave;
                if (gm?.Score != null)
                {
                    _scoreText.text = "SCORE: " + gm.Score.Score.ToString("N0");
                    if (gm.Score.IsNewBest)
                    {
                        _bestText.text = "NEW BEST!";
                        _bestText.color = GameConfig.EmberOrange;
                    }
                    else
                    {
                        _bestText.text = "BEST: " + gm.Score.Best.ToString("N0");
                        _bestText.color = UITheme.TextPrimary;
                    }
                }
            }
        }
    }
}
