using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// A pointer-driven button that reports press and release separately, so the same component
    /// serves both tap actions (PUNCH/SWORD/menu - use OnDown) and held actions (LEFT/RIGHT/BLOCK
    /// - use OnDown + OnUp). Releases on pointer-up AND pointer-exit so a finger sliding off the
    /// button correctly ends a hold. Gives light press feedback (scale + opacity) and — on
    /// menu-family buttons only (<see cref="UseUiFeedback"/>) — a UI click sound + light haptic.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnDown;
        public Action OnUp;

        /// <summary>Menu / pause / game-over / settings + the HUD pause button set this true for a
        /// click SFX + light haptic. Combat controls leave it false — they already trigger combat
        /// SFX and hits already rumble, so double-feedback (and a per-move buzz) feels bad.</summary>
        public bool UseUiFeedback = false;

        /// <summary>When false the button ignores input and dims to <see cref="UITheme.DisabledAlpha"/>.</summary>
        public bool Interactable = true;

        /// <summary>Optional graphic shown only while the button is held — the "active" ring on the
        /// hold-type controls (LEFT / RIGHT / BLOCK) so holds are visually discoverable.</summary>
        public Graphic Highlight;

        private Graphic _graphic;
        private RectTransform _rt;
        private bool _pressed;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _rt = GetComponent<RectTransform>();
            SetVisual(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable) return;
            _pressed = true;
            SetVisual(true);
            if (UseUiFeedback)
            {
                GameManager.Instance?.Sfx?.Play(SfxManager.UiButton, 1f, vary: false);
                Rumble.Light();
            }
            OnDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            SetVisual(false);
            OnUp?.Invoke();
        }

        /// <summary>Toggle interactivity and reflect it visually (releases an in-progress hold).</summary>
        public void SetInteractable(bool on)
        {
            Interactable = on;
            if (!on && _pressed) Release();
            else SetVisual(_pressed);
        }

        private void SetVisual(bool pressed)
        {
            if (_graphic != null)
            {
                Color c = _graphic.color;
                c.a = !Interactable ? UITheme.DisabledAlpha
                    : (pressed ? UITheme.ActiveAlpha : UITheme.IdleAlpha);
                _graphic.color = c;
            }
            if (_rt != null) _rt.localScale = pressed ? Vector3.one * UITheme.PressScale : Vector3.one;
            if (Highlight != null) Highlight.enabled = pressed && Interactable;
        }
    }
}
