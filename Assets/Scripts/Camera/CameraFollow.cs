using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Side-locked combat camera. Follows the player's X with a smooth damp plus a facing
    /// look-ahead, keeps a fixed Y/Z and pitch, clamps to the lane — and layers on the feel
    /// systems: trauma-based Perlin shake (offset + roll, trauma^2 falloff, unscaled time so
    /// it reads through hit-stop) and a kill punch-in dolly on Z. Shake and look-ahead are
    /// tuned so the player never leaves the central readable band on a phone screen.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float OffsetY = 4.2f;
        public float OffsetZ = -11f;
        public float Pitch = 11f;
        public float SmoothTime = 0.15f;
        public float ClampMargin = 3f; // how far past the lane the camera centre may travel

        private float _vel;
        private float _lookAhead, _lookVel;
        private float _trauma, _pendingTrauma;
        private float _punch, _punchVel, _punchTarget;
        private float _noiseT;

        public void Configure(Transform target)
        {
            Target = target;
            transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);
            if (target != null)
            {
                transform.position = new Vector3(ClampX(target.position.x), OffsetY, OffsetZ);
            }
        }

        /// <summary>Add camera trauma (0..1). Multiple calls per frame keep only the strongest.</summary>
        public void AddTrauma(float amount)
        {
            _pendingTrauma = Mathf.Max(_pendingTrauma, amount);
        }

        /// <summary>Brief dolly toward the action (kills / finishers). strength01 scales the depth.</summary>
        public void KillPunch(float strength01)
        {
            _punchTarget = Mathf.Max(_punchTarget, GameConfig.KillPunchInZ * Mathf.Clamp01(strength01));
        }

        private float ClampX(float x)
        {
            return Mathf.Clamp(x, GameConfig.LaneMinX - ClampMargin, GameConfig.LaneMaxX + ClampMargin);
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            float udt = Time.unscaledDeltaTime;

            // Follow + look-ahead in facing direction.
            var gm = GameManager.Instance;
            float facing = gm != null && gm.Player != null ? gm.Player.FacingSign : 0f;
            bool playing = gm != null && gm.State == GameState.Playing;
            _lookAhead = Mathf.SmoothDamp(_lookAhead, playing ? facing * GameConfig.CameraLookAhead : 0f,
                ref _lookVel, GameConfig.CameraLookAheadSmooth, Mathf.Infinity, udt);
            float desired = ClampX(Target.position.x + _lookAhead);
            float x = Mathf.SmoothDamp(transform.position.x, desired, ref _vel, SmoothTime, Mathf.Infinity, udt);

            // Trauma shake (trauma^2 so small hits stay subtle, big hits bite).
            _trauma = Mathf.Clamp01(_trauma + _pendingTrauma);
            _pendingTrauma = 0f;
            _trauma = Mathf.Max(0f, _trauma - GameConfig.TraumaDecayPerSec * udt);
            float shake = _trauma * _trauma;
            _noiseT += udt * GameConfig.ShakeFreq;
            float offX = (Mathf.PerlinNoise(_noiseT, 0.37f) * 2f - 1f) * GameConfig.ShakeMaxOffset * shake;
            float offY = (Mathf.PerlinNoise(0.71f, _noiseT) * 2f - 1f) * GameConfig.ShakeMaxOffset * shake;
            float roll = (Mathf.PerlinNoise(_noiseT, 9.13f) * 2f - 1f) * GameConfig.ShakeMaxRoll * shake;

            // Kill punch-in: snaps in via smooth damp, releases over KillPunchOutTime.
            _punchTarget = Mathf.MoveTowards(_punchTarget, 0f,
                GameConfig.KillPunchInZ / GameConfig.KillPunchOutTime * udt);
            _punch = Mathf.SmoothDamp(_punch, _punchTarget, ref _punchVel, 0.06f, Mathf.Infinity, udt);

            transform.position = new Vector3(x + offX, OffsetY + offY, OffsetZ + _punch);
            transform.rotation = Quaternion.Euler(Pitch, 0f, roll);
        }
    }
}
