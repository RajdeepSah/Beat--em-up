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
    /// button correctly ends a hold. Gives light press feedback (scale + opacity).
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnDown;
        public Action OnUp;

        private Graphic _graphic;
        private RectTransform _rt;
        private bool _pressed;
        private float _idleAlpha = 0.72f;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _rt = GetComponent<RectTransform>();
            SetVisual(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            SetVisual(true);
            OnDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            SetVisual(false);
            OnUp?.Invoke();
        }

        private void SetVisual(bool pressed)
        {
            if (_graphic != null)
            {
                Color c = _graphic.color;
                c.a = pressed ? 1f : _idleAlpha;
                _graphic.color = c;
            }
            if (_rt != null) _rt.localScale = pressed ? Vector3.one * 0.92f : Vector3.one;
        }
    }
}
