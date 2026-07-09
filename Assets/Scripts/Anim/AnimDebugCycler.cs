using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Development-build-only clip inspector (IMGUI so it needs zero canvas wiring): cycle
    /// through the hero's bound clips, play or scrub them, and read clip length + current
    /// normalized time. This is the tool used to measure each attack clip's contact frame
    /// (ActiveStartNorm/ActiveEndNorm) which is then hard-coded into GameConfig's ClipDefs.
    /// Toggle with a 4-finger tap (device) or F9 (editor). Added by Bootstrap in debug builds.
    /// </summary>
    public class AnimDebugCycler : MonoBehaviour
    {
        private bool _visible;
        private int _index;
        private float _scrub;
        private bool _playing = true;

        private static readonly string[] Keys =
            { "Idle", "Walk", "Punch1", "Punch2", "Punch3", "Sword", "Block", "Roll", "Hit", "Knockdown", "Die" };

        private ActorAnimator Animator()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return null;
            return gm.Player.DebugAnimator;
        }

        private void Update()
        {
            if (Input.touchCount == 4 && Input.GetTouch(3).phase == TouchPhase.Began) _visible = !_visible;
            if (Input.GetKeyDown(KeyCode.F9)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            var anim = Animator();

            GUILayout.BeginArea(new Rect(10, 10, 420, 240), GUI.skin.box);
            GUILayout.Label(anim == null ? "ActorAnimator: NOT BOUND (procedural fallback active)"
                                         : "ActorAnimator: READY");
            if (anim != null)
            {
                string key = Keys[_index];
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<")) _index = (_index + Keys.Length - 1) % Keys.Length;
                GUILayout.Label($"{key}  (len {anim.ClipLength(key):0.00}s, bound {anim.Has(key)})",
                    GUILayout.Width(240));
                if (GUILayout.Button(">")) _index = (_index + 1) % Keys.Length;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_playing ? "SCRUB" : "PLAY")) _playing = !_playing;
                GUILayout.EndHorizontal();

                if (_playing)
                {
                    if (GUILayout.Button("Trigger")) anim.Retrigger(key);
                }
                else
                {
                    float prev = _scrub;
                    _scrub = GUILayout.HorizontalSlider(_scrub, 0f, 1f);
                    GUILayout.Label($"normalizedTime {_scrub:0.000}");
                    if (!Mathf.Approximately(prev, _scrub)) anim.Scrub(key, _scrub);
                }
            }
            GUILayout.EndArea();
        }
    }
}
