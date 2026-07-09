using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Per-actor skeletal clip player over legacy UnityEngine.Animation (the only clip type
    /// glTFast yields at runtime; also the cheapest evaluator on Android). Binds the shared
    /// AnimLibrary clips onto ONE character instance, verifies each clip actually moves the
    /// rig (legacy clips bind by node-path string and fail SILENTLY on mismatch — the probe
    /// catches that and drops the clip so the actor falls back to procedural animation), and
    /// exposes CrossFade for states plus Scrub for phase-locked attacks: gameplay timers stay
    /// authoritative and the clip's contact frame is warped onto the Active phase.
    /// </summary>
    public class ActorAnimator
    {
        private Animation _anim;
        private Transform _visual;
        private readonly Dictionary<string, GameConfig.ClipDef> _defs = new Dictionary<string, GameConfig.ClipDef>();
        private readonly HashSet<string> _bound = new HashSet<string>();
        private string _idleAlias;   // baked base-model clip used when no Idle file is bound
        private string _current;

        public bool Ready { get; private set; }
        public bool Has(string key) => _bound.Contains(key) || (key == "Idle" && _idleAlias != null);

        /// <summary>
        /// Bind a clip set onto the instantiated GLB under <paramref name="visual"/>.
        /// Ready requires at least Idle + Walk so locomotion never half-upgrades.
        /// </summary>
        public bool TryBind(Transform visual, GameConfig.ClipDef[] set)
        {
            Ready = false;
            _bound.Clear();
            _defs.Clear();
            _idleAlias = null;
            _current = null;
            _visual = visual;
            if (visual == null) return false;

            // glTFast's default instantiator puts an Animation component (with the baked clip)
            // on the scene root it creates; otherwise add our own there.
            _anim = visual.GetComponentInChildren<Animation>(true);
            if (_anim == null)
            {
                Transform root = visual.childCount > 0 ? visual.GetChild(0) : visual;
                _anim = root.gameObject.AddComponent<Animation>();
            }
            _anim.playAutomatically = false;
            _anim.Stop();

            // Remember the baked clip (the base GLB ships with Idle baked in).
            foreach (AnimationState st in _anim)
            {
                _idleAlias = st.name;
                st.wrapMode = WrapMode.Loop;
                break;
            }

            var bones = _anim.transform.GetComponentsInChildren<Transform>(true);

            foreach (var def in set)
            {
                _defs[def.Key] = def;
                var clip = AnimLibrary.Get(def.File);
                if (clip == null) continue;
                _anim.AddClip(clip, def.Key);
                if (ProbeMoves(def.Key, bones))
                {
                    _bound.Add(def.Key);
                }
                else
                {
                    _anim.RemoveClip(def.Key);
                    Debug.LogWarning($"[ActorAnimator] Clip '{def.File}' does not bind to this rig (node-path mismatch) — dropped.");
                }
            }

            Ready = Has("Idle") && Has("Walk");
            if (Ready) CrossFade("Idle");
            return Ready;
        }

        /// <summary>Sample the clip mid-way and check that at least one bone moved.</summary>
        private bool ProbeMoves(string key, Transform[] bones)
        {
            var state = _anim[key];
            if (state == null || state.length <= 0f) return false;

            var pos = new Vector3[bones.Length];
            var rot = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                pos[i] = bones[i].localPosition;
                rot[i] = bones[i].localRotation;
            }

            state.enabled = true;
            state.weight = 1f;
            state.normalizedTime = 0.3f;
            _anim.Sample();
            state.enabled = false;

            bool moved = false;
            for (int i = 0; i < bones.Length; i++)
            {
                if ((bones[i].localPosition - pos[i]).sqrMagnitude > 1e-8f ||
                    Quaternion.Angle(bones[i].localRotation, rot[i]) > 0.05f)
                {
                    moved = true;
                }
                bones[i].localPosition = pos[i];   // restore the probed pose
                bones[i].localRotation = rot[i];
            }
            return moved;
        }

        private string Resolve(string key)
        {
            if (_bound.Contains(key)) return key;
            if (key == "Idle" && _idleAlias != null) return _idleAlias;
            return null;
        }

        /// <summary>Crossfade to a state clip. Looping states never restart on repeat calls.</summary>
        public void CrossFade(string key, float fadeOverride = -1f)
        {
            string resolved = Resolve(key);
            if (resolved == null || _anim == null) return;
            if (_current == resolved) return;

            float fade = fadeOverride >= 0f ? fadeOverride
                : _defs.TryGetValue(key, out var def) ? def.Fade : 0.1f;
            var st = _anim[resolved];
            if (st != null)
            {
                st.speed = 1f;
                if (st.wrapMode == WrapMode.Once || st.wrapMode == WrapMode.ClampForever)
                    st.time = 0f;  // one-shots always retrigger from the top
            }
            _anim.CrossFade(resolved, fade);
            _current = resolved;
        }

        /// <summary>Retrigger a one-shot even if it is already the current state (hit reacts).</summary>
        public void Retrigger(string key, float fadeOverride = -1f)
        {
            string resolved = Resolve(key);
            if (resolved == null || _anim == null) return;
            var st = _anim[resolved];
            if (st != null) { st.time = 0f; st.speed = 1f; }
            float fade = fadeOverride >= 0f ? fadeOverride
                : _defs.TryGetValue(key, out var def) ? def.Fade : 0.05f;
            _anim.CrossFade(resolved, fade);
            _current = resolved;
        }

        /// <summary>Pin a clip at a normalized time (gameplay timers drive, the clip follows).</summary>
        public void Scrub(string key, float normalizedTime)
        {
            string resolved = Resolve(key);
            if (resolved == null || _anim == null) return;
            if (_current != resolved)
            {
                _anim.CrossFade(resolved, 0.03f);
                _current = resolved;
            }
            var st = _anim[resolved];
            if (st == null) return;
            st.speed = 0f;
            st.normalizedTime = Mathf.Clamp01(normalizedTime);
        }

        /// <summary>
        /// Map an attack's Startup/Active/Recovery progress onto the clip so the visual
        /// contact frame (ActiveStartNorm) lands exactly when the hitbox fires.
        /// </summary>
        public void ScrubAttack(string key, CombatSystem.AttackPhase phase, float t01)
        {
            if (!_defs.TryGetValue(key, out var def)) return;
            float norm;
            switch (phase)
            {
                case CombatSystem.AttackPhase.Startup:
                    norm = Mathf.Lerp(0f, def.ActiveStartNorm, t01); break;
                case CombatSystem.AttackPhase.Active:
                    norm = Mathf.Lerp(def.ActiveStartNorm, def.ActiveEndNorm, t01); break;
                case CombatSystem.AttackPhase.Recovery:
                    norm = Mathf.Lerp(def.ActiveEndNorm, 1f, t01); break;
                default:
                    return;
            }
            Scrub(key, norm);
        }

        /// <summary>Same normalized mapping, driven by an enemy's windup/followthrough timers.</summary>
        public void ScrubWindow(string key, float fromNorm, float toNorm, float t01)
        {
            Scrub(key, Mathf.Lerp(fromNorm, toNorm, Mathf.Clamp01(t01)));
        }

        public float ClipLength(string key)
        {
            string resolved = Resolve(key);
            if (resolved == null || _anim == null) return 0f;
            var st = _anim[resolved];
            return st != null ? st.length : 0f;
        }

        /// <summary>Override a state's playback speed (e.g. fit a death clip into the corpse window).</summary>
        public void SetStateSpeed(string key, float speed)
        {
            string resolved = Resolve(key);
            if (resolved == null || _anim == null) return;
            var st = _anim[resolved];
            if (st != null) st.speed = speed;
        }

        public void ResetToIdle()
        {
            _current = null;
            if (Ready) CrossFade("Idle", 0f);
        }
    }
}
