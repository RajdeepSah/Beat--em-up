using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Pause overlay (section 13): PAUSED, RESUME, RESTART, QUIT TO MENU, best score.</summary>
    public class PauseController : MonoBehaviour
    {
        private RectTransform _root;
        private Text _bestText;

        public void Build(Transform canvas, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("Pause", canvas);
            UIFactory.Image("PauseScrim", _root, null, new Color(0f, 0f, 0f, 0.7f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);

            UIFactory.Label("PausedTitle", _root, "PAUSED", 80, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(800, 100));

            var resume = UIFactory.PanelButton("Resume", _root, "RESUME", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(440, 100), panelSprite);
            resume.OnDown = () => GameManager.Instance?.ResumeGame();

            var restart = UIFactory.PanelButton("RestartP", _root, "RESTART", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(440, 100), panelSprite);
            restart.OnDown = () => GameManager.Instance?.Restart();

            var quit = UIFactory.PanelButton("QuitP", _root, "QUIT TO MENU", 36,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(440, 90), panelSprite);
            quit.OnDown = () => GameManager.Instance?.QuitToMenu();

            _bestText = UIFactory.Label("PauseBest", _root, "BEST: 0", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.85f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(600, 40));

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            OnStateChanged(gm != null ? gm.State : GameState.Menu);
        }

        private void OnStateChanged(GameState s)
        {
            bool paused = s == GameState.Paused;
            if (_root != null) _root.gameObject.SetActive(paused);
            if (paused)
            {
                var score = GameManager.Instance?.Score;
                if (_bestText != null && score != null) _bestText.text = "BEST: " + score.Best.ToString("N0");
            }
        }
    }
}
