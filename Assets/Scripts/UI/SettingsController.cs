using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Settings / accessibility overlay: master / music / SFX volume, camera-shake intensity and a
    /// haptics toggle, each wired live to <see cref="SettingsService"/> (which applies + persists).
    /// A local overlay (not a GameState) reached from the Menu and Pause screens; fades via UITransition
    /// and, as a safety net, closes on any game-state change. Built in code by Bootstrap.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        public static SettingsController Instance { get; private set; }

        private RectTransform _root;
        private UITransition _transition;

        public void Build(Transform canvas, Sprite panelSprite)
        {
            Instance = this;
            _root = UIFactory.FullScreen("Settings", canvas);
            _root.gameObject.AddComponent<CanvasGroup>();
            _transition = _root.gameObject.AddComponent<UITransition>();

            var scrim = UIFactory.Image("SettingsScrim", _root, null, UITheme.Scrim,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true);
            scrim.rectTransform.offsetMin = Vector2.zero; scrim.rectTransform.offsetMax = Vector2.zero;

            var safe = UILayout.SafeAreaRoot("SettingsSafe", _root);

            var panel = UIFactory.Image("SettingsPanel", safe, panelSprite, panelSprite != null ? Color.white : GameConfig.Iron,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 660),
                raycast: true, sliced: panelSprite != null);
            Transform content = panel.transform;

            UIFactory.Label("SettingsTitle", content, "SETTINGS", UITheme.H2, TextAnchor.MiddleCenter, UITheme.Accent,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(600, 70));

            const float w = 640f;
            float y = -158f; const float dy = -66f;
            UIFactory.SliderRow("SetMaster", content, "MASTER VOLUME", SettingsService.Master, new Vector2(0, y), w, SettingsService.SetMaster); y += dy;
            UIFactory.SliderRow("SetMusic",  content, "MUSIC",         SettingsService.Music,  new Vector2(0, y), w, SettingsService.SetMusic);  y += dy;
            UIFactory.SliderRow("SetSfx",    content, "SOUND FX",      SettingsService.Sfx,    new Vector2(0, y), w, SettingsService.SetSfx);    y += dy;
            UIFactory.SliderRow("SetShake",  content, "SCREEN SHAKE",  SettingsService.Shake,  new Vector2(0, y), w, SettingsService.SetShake);  y += dy;
            UIFactory.ToggleRow("SetHaptics", content, "HAPTICS",      SettingsService.Haptics, new Vector2(0, y), w, SettingsService.SetHaptics);

            var back = UIFactory.PanelButton("SettingsBack", content, "BACK", UITheme.Btn,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(300, 96), panelSprite);
            back.OnDown = Close;

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += _ => Close();   // safety: never linger across a state change

            _transition.SetShownImmediate(false);
        }

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public void Open() => _transition?.Show();
        public void Close() { if (IsOpen) _transition?.Hide(); }
    }
}
