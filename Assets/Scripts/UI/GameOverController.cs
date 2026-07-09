using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Game-over screen (section 13): THE KEEP HAS FALLEN, wave reached, score, best, RESTART, MAIN MENU.</summary>
    public class GameOverController : MonoBehaviour
    {
        private RectTransform _root;
        private Text _waveText, _scoreText, _bestText;

        public void Build(Transform canvas, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("GameOver", canvas);
            UIFactory.Image("GoScrim", _root, null, new Color(0.04f, 0.05f, 0.08f, 0.88f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);

            UIFactory.Label("GoTitle", _root, "THE KEEP HAS FALLEN", 70, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(1400, 100));

            _waveText = UIFactory.Label("GoWave", _root, "WAVE REACHED: 1", 40, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(900, 56));
            _scoreText = UIFactory.Label("GoScore", _root, "SCORE: 0", 52, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(900, 70));
            _bestText = UIFactory.Label("GoBest", _root, "BEST: 0", 36, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(900, 50));

            var restart = UIFactory.PanelButton("RestartGo", _root, "RESTART", 44,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(440, 100), panelSprite);
            restart.OnDown = () => GameManager.Instance?.Restart();

            var menu = UIFactory.PanelButton("MenuGo", _root, "MAIN MENU", 38,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(440, 90), panelSprite);
            menu.OnDown = () => GameManager.Instance?.QuitToMenu();

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            OnStateChanged(gm != null ? gm.State : GameState.Menu);
        }

        private void OnStateChanged(GameState s)
        {
            bool over = s == GameState.GameOver;
            if (_root != null) _root.gameObject.SetActive(over);
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
                        _bestText.color = new Color(1, 1, 1, 0.9f);
                    }
                }
            }
        }
    }
}
