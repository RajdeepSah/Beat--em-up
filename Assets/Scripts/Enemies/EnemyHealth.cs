using UnityEngine;

namespace Ironhold
{
    /// <summary>Enemy hit points + damage entry point. Delegates reaction/death to EnemyBase.</summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        public Faction Faction => Faction.Enemy;
        public bool IsAlive { get; private set; }
        public Transform Transform => transform;

        private EnemyBase _base;
        private float _hp;

        public void Init(float maxHp)
        {
            _hp = maxHp;
            IsAlive = true;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Attacker == Faction.Enemy) return;
            if (_base == null) _base = GetComponent<EnemyBase>();

            float amount = info.Amount;
            if (_base != null && _base.IsKnockedDown) amount *= GameConfig.OTGDamageMult; // on-the-ground hit

            _hp -= amount;
            DamageNumbers.Damage(transform.position + Vector3.up * 1.7f, amount, info.IsHeavy);
            bool stillAlive = _hp > 0f;
            _base?.ReactToHit(info, stillAlive);

            if (!stillAlive)
            {
                IsAlive = false;
                _base?.OnKilled(info);
            }
        }
    }
}
