using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>Main menu (section 13): title art, PLAY, HOW TO PLAY overlay, and the best score.</summary>
    public class MenuController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _howTo;
        private Text _bestText;

        public void Build(Transform canvas, Sprite titleSprite, Sprite panelSprite)
        {
            _root = UIFactory.FullScreen("MainMenu", canvas);

            UIFactory.Image("Bg", _root, titleSprite, titleSprite != null ? Color.white : GameConfig.NightBlue,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            // Dark scrim for text legibility over the art.
            UIFactory.Image("Scrim", _root, null, new Color(0f, 0f, 0f, 0.35f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            UIFactory.Label("Title", _root, "IRONHOLD", 120, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1200, 140));
            UIFactory.Label("Subtitle", _root, "ENDLESS SIEGE", 60, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(1200, 80));
            UIFactory.Label("Tagline", _root, "How long can you hold the line?", 30, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.85f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -360), new Vector2(1000, 50));

            var play = UIFactory.PanelButton("Play", _root, "PLAY", 46,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(420, 110), panelSprite);
            play.OnDown = () => GameManager.Instance?.StartRun();

            var howToBtn = UIFactory.PanelButton("HowToBtn", _root, "HOW TO PLAY", 32,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(420, 84), panelSprite);
            howToBtn.OnDown = () => _howTo.gameObject.SetActive(true);

            _bestText = UIFactory.Label("Best", _root, "BEST: 0", 30, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(600, 50));

            BuildHowTo(panelSprite);

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            OnStateChanged(gm != null ? gm.State : GameState.Menu);
        }

        private void BuildHowTo(Sprite panelSprite)
        {
            _howTo = UIFactory.FullScreen("HowTo", _root);
            UIFactory.Image("HowToScrim", _howTo, null, new Color(0f, 0f, 0f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);
            UIFactory.Label("HowToTitle", _howTo, "HOW TO PLAY", 56, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 80));
            UIFactory.Label("HowToBody", _howTo,
                "MOVE with  <  and  > .\nPUNCH for fast hits.\nSWORD for heavy hits.\nHold BLOCK to guard.\n\nSurvive the waves.\nThere is no end - only your score.",
                34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1100, 420));
            var back = UIFactory.PanelButton("HowToBack", _howTo, "BACK", 36,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(320, 90), panelSprite);
            back.OnDown = () => _howTo.gameObject.SetActive(false);
            _howTo.gameObject.SetActive(false);
        }

        private void OnStateChanged(GameState s)
        {
            bool menu = s == GameState.Menu;
            if (_root != null) _root.gameObject.SetActive(menu);
            if (menu)
            {
                if (_howTo != null) _howTo.gameObject.SetActive(false);
                var score = GameManager.Instance?.Score;
                if (_bestText != null && score != null) _bestText.text = "BEST: " + score.Best.ToString("N0");
            }
        }
    }
}
