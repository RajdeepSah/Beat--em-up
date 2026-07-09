using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Player combat state machine. Attacks run Startup -> Active -> Recovery from an
    /// AttackDef table (3-hit light chain + sword, with a faster knockdown FINISHER when
    /// sword cancels a light). Input is buffered for InputBufferSeconds in ANY phase and
    /// consumed at the earliest legal cancel point: recovery cancels earlier when the hit
    /// LANDED (hit-confirm reward), dodge/block have their own cancel windows, Startup and
    /// Active never cancel (commitment). Dodge is a roll with i-frames; getting floored by
    /// Launch/Knockdown hits puts the player in a real knockdown -> getup cycle. BLOCK is a
    /// held stance that drains stamina; guard break stuns. Exposes read-only state that
    /// PlayerController and the skeletal ActorAnimator consume.
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        public enum AttackPhase { Ready, Startup, Active, Recovery }
        public enum ActionState { Normal, Dodging, KnockedDown, Getup }

        public AttackPhase Phase { get; private set; } = AttackPhase.Ready;
        public ActionState Action { get; private set; } = ActionState.Normal;
        public float PhaseT01 { get; private set; }
        public GameConfig.AttackDef CurrentAttack { get; private set; }
        public int ChainIndex { get; private set; }

        public bool IsStunned => _stunTimer > 0f;
        public bool IsAttacking => Phase != AttackPhase.Ready;
        public bool IsBlocking => _blockHeld && Phase == AttackPhase.Ready && Action == ActionState.Normal && !IsStunned;
        public bool IsDodging => Action == ActionState.Dodging;
        public bool IsKnockedDown => Action == ActionState.KnockedDown || Action == ActionState.Getup;
        public bool CurrentHeavy => IsAttacking && CurrentAttack.IsHeavyFx;
        public float DodgeDir { get; private set; } = 1f;
        public float DodgeT01 => Action == ActionState.Dodging ? Mathf.Clamp01(_actionT / GameConfig.DodgeDuration) : 0f;
        public float KnockdownT01 => Action == ActionState.KnockedDown ? Mathf.Clamp01(_actionT / GameConfig.KnockdownDuration) : 0f;
        public float GetupT01 => Action == ActionState.Getup ? Mathf.Clamp01(_actionT / GameConfig.GetupDuration) : 0f;
        public bool IsDodgeInvuln => Action == ActionState.Dodging &&
            _actionT >= GameConfig.DodgeIframeStart && _actionT <= GameConfig.DodgeIframeEnd;
        public bool IsGetupInvuln => Action == ActionState.Getup;
        public bool CanAct => Phase == AttackPhase.Ready && !IsStunned && Action == ActionState.Normal;
        public bool CanMove => CanAct && !IsBlocking;

        private enum Buffered { None, Light, Heavy, Dodge }

        private PlayerController _controller;
        private PlayerStamina _stamina;

        private float _phaseDuration, _phaseTimer;
        private bool _hitApplied;
        private bool _blockHeld, _wasBlocking;
        private float _stunTimer;

        private Buffered _buffered;
        private float _bufferAge;
        private float _bufferedDodgeDir;

        private float _actionT;          // elapsed inside Dodging / KnockedDown / Getup
        private float _dodgeCd;

        // Chain bookkeeping: what the last attack was, whether it landed, and for how long
        // that memory keeps the chain alive after recovery ends.
        private bool _currentIsLight;
        private bool _lastLanded;
        private bool _memoryIsLight;
        private bool _memoryLanded;
        private int _memoryIndex;
        private float _chainMemoryTimer;

        // Resolved in Start (not Awake): PlayerController <-> CombatSystem is a circular
        // dependency, so no AddComponent order satisfies Awake-time lookups.
        private void Start()
        {
            _controller = GetComponent<PlayerController>();
            _stamina = GetComponent<PlayerStamina>();
        }

        public void ResetActor()
        {
            Phase = AttackPhase.Ready;
            Action = ActionState.Normal;
            PhaseT01 = 0f;
            _phaseTimer = 0f;
            _stunTimer = 0f;
            _blockHeld = false;
            _wasBlocking = false;
            _buffered = Buffered.None;
            _actionT = 0f;
            _dodgeCd = 0f;
            ChainIndex = 0;
            _chainMemoryTimer = 0f;
            _lastLanded = false;
        }

        // ---- Input entry points (any phase; consumed at the earliest legal moment) ----

        public void TryAttack(bool heavy)
        {
            if (IsKnockedDown) return; // never buffer across a knockdown
            _buffered = heavy ? Buffered.Heavy : Buffered.Light;
            _bufferAge = 0f;
        }

        public void TryDodge(float dir)
        {
            if (IsKnockedDown) return;
            _buffered = Buffered.Dodge;
            _bufferedDodgeDir = dir;
            _bufferAge = 0f;
        }

        /// <summary>When the block was last PRESSED (parry window checks against this).</summary>
        public float BlockHeldSince { get; private set; } = -999f;

        public void SetBlock(bool held)
        {
            if (held && !_blockHeld) BlockHeldSince = Time.time;
            _blockHeld = held;
        }

        public void GuardBreak()
        {
            _blockHeld = false;
            _buffered = Buffered.None;
            Phase = AttackPhase.Ready;
            _stunTimer = GameConfig.GuardBreakStun;
            Impact.GuardBreakFeedback();
            var gm = GameManager.Instance;
            gm?.Sfx?.Play(SfxManager.GuardBreak);
            gm?.Announcer?.Play(AnnouncerVO.Line.GuardBreak);
        }

        /// <summary>An unblocked hit landed on the player. Startup is interruptible; Active/Recovery are not.</summary>
        public void NotifyHurt()
        {
            _buffered = Buffered.None;
            if (Phase == AttackPhase.Startup)
            {
                Phase = AttackPhase.Ready;
                PhaseT01 = 0f;
                _stunTimer = Mathf.Max(_stunTimer, GameConfig.PlayerHitstun);
            }
        }

        /// <summary>A Launch/Knockdown-class hit floored the player.</summary>
        public void ForceKnockdown()
        {
            Phase = AttackPhase.Ready;
            PhaseT01 = 0f;
            _stunTimer = 0f;
            _buffered = Buffered.None;
            Action = ActionState.KnockedDown;
            _actionT = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _bufferAge += dt;
            if (_buffered != Buffered.None && _bufferAge > GameConfig.InputBufferSeconds)
                _buffered = Buffered.None;
            if (_dodgeCd > 0f) _dodgeCd -= dt;
            if (_chainMemoryTimer > 0f) _chainMemoryTimer -= dt;

            // Grounded action states run their own clock and suppress everything else.
            if (Action == ActionState.KnockedDown)
            {
                _actionT += dt;
                if (_actionT >= GameConfig.KnockdownDuration) { Action = ActionState.Getup; _actionT = 0f; }
                PhaseT01 = 0f;
                return;
            }
            if (Action == ActionState.Getup)
            {
                _actionT += dt;
                if (_actionT >= GameConfig.GetupDuration) Action = ActionState.Normal;
                PhaseT01 = 0f;
                return;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                PhaseT01 = 0f;
                return;
            }

            if (Action == ActionState.Dodging)
            {
                _actionT += dt;
                if (_actionT >= GameConfig.DodgeDuration)
                {
                    Action = ActionState.Normal;
                    _dodgeCd = GameConfig.DodgeCooldown;
                }
                TryConsumeBuffer(); // roll-cancel into a buffered attack near the end
                return;
            }

            // Block stance: raise sfx on engage, drain while held.
            if (IsBlocking && !_wasBlocking) GameManager.Instance?.Sfx?.Play(SfxManager.BlockRaise);
            _wasBlocking = IsBlocking;
            if (IsBlocking && _stamina != null)
            {
                if (_stamina.DrainContinuous(GameConfig.BlockStaminaPerSec)) GuardBreak();
            }

            // Attack phase machine (timers authoritative; clips conform elsewhere).
            if (Phase != AttackPhase.Ready)
            {
                _phaseTimer -= dt;
                PhaseT01 = 1f - Mathf.Clamp01(_phaseTimer / _phaseDuration);

                switch (Phase)
                {
                    case AttackPhase.Startup:
                        if (_phaseTimer <= 0f) EnterPhase(AttackPhase.Active, CurrentAttack.Active);
                        break;

                    case AttackPhase.Active:
                        if (!_hitApplied) ApplyHit();
                        if (_phaseTimer <= 0f) EnterPhase(AttackPhase.Recovery, CurrentAttack.Recovery);
                        break;

                    case AttackPhase.Recovery:
                        // Block-cancel: holding block cuts the tail of recovery.
                        if (_blockHeld && PhaseT01 >= GameConfig.BlockCancelPct)
                        {
                            EndAttack();
                            break;
                        }
                        if (_phaseTimer <= 0f) EndAttack();
                        break;
                }
            }
            else
            {
                PhaseT01 = 0f;
            }

            TryConsumeBuffer();
        }

        private void EndAttack()
        {
            Phase = AttackPhase.Ready;
            PhaseT01 = 0f;
            _memoryIsLight = _currentIsLight;
            _memoryLanded = _lastLanded;
            _memoryIndex = ChainIndex;
            _chainMemoryTimer = GameConfig.ChainResetAfter;
        }

        private void TryConsumeBuffer()
        {
            if (_buffered == Buffered.None || IsStunned) return;

            if (_buffered == Buffered.Dodge)
            {
                if (Action == ActionState.Dodging) return;
                bool legal = CanAct || IsBlocking ||
                             (Phase == AttackPhase.Recovery && PhaseT01 >= GameConfig.DodgeCancelPct);
                if (legal && _dodgeCd <= 0f && (_stamina == null || _stamina.CanSpend(GameConfig.DodgeStamina)))
                {
                    _buffered = Buffered.None;
                    BeginDodge(_bufferedDodgeDir);
                }
                return;
            }

            bool heavy = _buffered == Buffered.Heavy;

            if (Action == ActionState.Dodging)
            {
                if (DodgeT01 >= GameConfig.DodgeAttackCancelPct)
                {
                    _buffered = Buffered.None;
                    Action = ActionState.Normal;
                    _dodgeCd = GameConfig.DodgeCooldown;
                    BeginAttack(heavy);
                }
                return;
            }

            if (_blockHeld) return; // no attacks from inside the guard

            if (Phase == AttackPhase.Ready)
            {
                _buffered = Buffered.None;
                BeginAttack(heavy);
            }
            else if (Phase == AttackPhase.Recovery)
            {
                float threshold = _lastLanded ? GameConfig.ChainCancelOnHitPct : GameConfig.ChainCancelWhiffPct;
                if (PhaseT01 >= threshold)
                {
                    _buffered = Buffered.None;
                    BeginAttack(heavy);
                }
            }
        }

        private void BeginAttack(bool heavy)
        {
            bool cancelling = IsAttacking;
            bool prevLight = cancelling ? _currentIsLight : _chainMemoryTimer > 0f && _memoryIsLight;
            bool prevLanded = cancelling ? _lastLanded : _chainMemoryTimer > 0f && _memoryLanded;
            int prevIndex = cancelling ? ChainIndex : _memoryIndex;

            GameConfig.AttackDef def;
            if (heavy)
            {
                // Sword inside a light's cancel window = the faster knockdown finisher.
                def = cancelling && prevLight && prevIndex < 2
                    ? GameConfig.HeroSwordFinisher
                    : GameConfig.HeroSword;
                ChainIndex = 0;
            }
            else
            {
                ChainIndex = prevLight && prevLanded ? Mathf.Min(prevIndex + 1, 2) : 0;
                def = GameConfig.HeroLightChain[ChainIndex];
            }

            if (_stamina != null && !_stamina.CanSpend(def.Stamina)) return;
            _stamina?.Spend(def.Stamina);

            CurrentAttack = def;
            _currentIsLight = !heavy;
            _hitApplied = false;
            _lastLanded = false;
            EnterPhase(AttackPhase.Startup, def.Startup);
            GameManager.Instance?.Sfx?.Play(heavy ? SfxManager.SwordSwing : SfxManager.PunchWhiff);
        }

        private void BeginDodge(float dir)
        {
            _stamina?.Spend(GameConfig.DodgeStamina);
            Phase = AttackPhase.Ready;
            PhaseT01 = 0f;
            Action = ActionState.Dodging;
            _actionT = 0f;
            DodgeDir = dir != 0f ? Mathf.Sign(dir) : (_controller != null ? _controller.FacingSign : 1f);
            GameManager.Instance?.Sfx?.Play(SfxManager.Dodge);
        }

        private void EnterPhase(AttackPhase phase, float duration)
        {
            Phase = phase;
            _phaseDuration = Mathf.Max(0.0001f, duration);
            _phaseTimer = _phaseDuration;
            PhaseT01 = 0f;
        }

        private void ApplyHit()
        {
            _hitApplied = true;
            var def = CurrentAttack;

            if (def.IsHeavyFx)
                SlashArc.Show(transform.position + Vector3.up * 1.2f, _controller.FacingSign);

            int hits = MeleeHitbox.ApplyMelee(
                transform.position, _controller.FacingSign, def.Reach, def.Damage, def.Knockback,
                def.Hit, Faction.Player, def.MaxTargets, this);

            if (hits > 0)
            {
                _lastLanded = true;
                _stamina?.Refund(GameConfig.StaminaRefundPerHit * hits);
                Impact.PlayerHitLanded(def.IsHeavyFx);
                var gm = GameManager.Instance;
                gm?.Score?.NotifyHit(hits);
                gm?.Sfx?.Play(def.IsHeavyFx ? SfxManager.SwordHit : SfxManager.PunchHit);
            }
        }
    }
}
