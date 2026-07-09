using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Anything that can be hit by a melee attack. Implemented by PlayerHealth and EnemyHealth.
    /// MeleeHitbox finds these via component lookup (no physics layers required).
    /// </summary>
    public interface IDamageable
    {
        Faction Faction { get; }
        bool IsAlive { get; }
        Transform Transform { get; }
        void TakeDamage(in DamageInfo info);
    }
}
