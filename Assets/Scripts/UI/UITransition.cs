using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Fade + slight scale-in for a screen root, on UNSCALED time (Pause sets timeScale 0, so scaled
    /// time would freeze the animation). Toggles the CanvasGroup's interactable/blocksRaycasts so a
    /// fading-out screen can't eat taps meant for the incoming one, and deactivates the GameObject when
    /// a hide finishes. Attach to a root that also has a CanvasGroup; drive with Show / Hide.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UITransition : MonoBehaviour
    {
        private CanvasGroup _cg;
        private RectTransform _rt;
        private float _target;        // desired alpha, 0 or 1
        private float _speed = 9f;    // alpha units per second (0->1 in ~0.11s)
        private float _fromScale = 0.97f;
        private bool _animating;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _rt = GetComponent<RectTransform>();
        }

        public void Show(float speed = 9f, float fromScale = 0.97f)
        {
            gameObject.SetActive(true);
            _speed = speed; _fromScale = fromScale; _target = 1f; _animating = true;
            _cg.blocksRaycasts = true;
            _cg.interactable = true;
        }

        public void Hide(float speed = 12f)
        {
            if (!gameObject.activeSelf) return;
            _speed = speed; _target = 0f; _animating = true;
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }

        /// <summary>Snap to a state with no animation (initial Build state).</summary>
        public void SetShownImmediate(bool shown)
        {
            if (_cg == null) { _cg = GetComponent<CanvasGroup>(); _rt = GetComponent<RectTransform>(); }
            _animating = false;
            _target = shown ? 1f : 0f;
            _cg.alpha = _target;
            _cg.blocksRaycasts = shown;
            _cg.interactable = shown;
            _rt.localScale = Vector3.one;
            gameObject.SetActive(shown);
        }

        private void Update()
        {
            if (!_animating) return;
            _cg.alpha = Mathf.MoveTowards(_cg.alpha, _target, _speed * Time.unscaledDeltaTime);
            // Scale grows with alpha on show; stays 1 on hide (pure fade out).
            float s = _target > 0.5f ? Mathf.Lerp(_fromScale, 1f, _cg.alpha) : 1f;
            _rt.localScale = new Vector3(s, s, 1f);
            if (Mathf.Approximately(_cg.alpha, _target))
            {
                _animating = false;
                _rt.localScale = Vector3.one;
                if (_target <= 0f) gameObject.SetActive(false);
            }
        }
    }
}
