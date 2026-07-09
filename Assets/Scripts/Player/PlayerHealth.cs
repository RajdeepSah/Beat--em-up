using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Player health and the damage pipeline (section 6). Applies block reduction (80% off when
    /// guarding from the front), i-frames after a hit, combo-break on damage, and the death flow.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public float HP { get; private set; }
        public float Max => GameConfig.PlayerMaxHP;
        public float Normalized => Mathf.Clamp01(HP / Max);

        public Faction Faction => Faction.Player;
        public bool IsAlive { get; private set; }
        public Transform Transform => transform;

        private CombatSystem _combat;
        private PlayerController _controller;
        private PlayerStamina _stamina;
        private float _invulnTimer;
        private bool _lowHpAnnounced;

        // Resolved in Start so all sibling components exist (see CombatSystem note).
        private void Start()
        {
            _combat = GetComponent<CombatSystem>();
            _controller = GetComponent<PlayerController>();
            _stamina = GetComponent<PlayerStamina>();
        }

        public void ResetActor()
        {
            HP = Max;
            IsAlive = true;
            _invulnTimer = 0f;
            _lowHpAnnounced = false;
        }

        private void Update()
        {
            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Attacker == Faction.Player) return;
            if (_invulnTimer > 0f) return;
            if (_combat != null && (_combat.IsDodgeInvuln || _combat.IsGetupInvuln)) return; // dodge / getup i-frames

            float damage = info.Amount;
            float dirToAttacker = Mathf.Sign(info.SourcePos.x - transform.position.x);
            bool blockingFront = _combat != null && _combat.IsBlocking &&
                                 Mathf.Approximately(dirToAttacker, _controller.FacingSign);

            var gm = GameManager.Instance;
            if (blockingFront)
            {
                // PARRY: block pressed just before the hit lands — negate everything, punish the attacker.
                if (Time.time - _combat.BlockHeldSince <= GameConfig.ParryWindow)
                {
                    _invulnTimer = 0.3f; // no multi-hit cheese through one parry
                    Impact.ParryFeedback();
                    HitSparks.Parry(transform.position + Vector3.up * 1.2f, dirToAttacker);
                    gm?.Sfx?.Play(SfxManager.Parry);
                    gm?.Score?.NotifyParry();
                    (info.SourceActor as EnemyBase)?.ApplyParryStagger();
                    return;
                }

                damage *= GameConfig.BlockDamageMultiplier;
                if (_stamina != null)
                {
                    _stamina.Spend(GameConfig.BlockHitStaminaCost);
                    if (_stamina.Current <= 0f) _combat.GuardBreak();
                }
                gm?.Sfx?.Play(SfxManager.BlockImpact);
            }
            else
            {
                gm?.Sfx?.Play(SfxManager.PlayerHurt);
                Impact.PlayerHurt();
                if (info.Hit == HitType.Knockdown || info.Hit == HitType.Launch)
                    _combat?.ForceKnockdown();
                else
                    _combat?.NotifyHurt();
            }

            HP = Mathf.Max(0f, HP - damage);
            _invulnTimer = GameConfig.PlayerInvulnTime;
            gm?.Score?.NotifyPlayerHit();
            _controller?.OnHitReaction(info, blockingFront);

            if (!_lowHpAnnounced && HP > 0f && HP <= GameConfig.LowHpThreshold)
            {
                _lowHpAnnounced = true;
                gm?.Announcer?.Play(AnnouncerVO.Line.LowHP);
            }

            if (HP <= 0f) Die();
        }

        private void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            var gm = GameManager.Instance;
            gm?.Sfx?.Play(SfxManager.PlayerDie);
            _controller?.OnDeath();
            gm?.OnPlayerDied();
        }
    }
}
