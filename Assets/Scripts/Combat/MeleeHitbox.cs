using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Shared melee resolution for both the player and enemies. Casts a Physics.OverlapBox
    /// in front of the attacker (along its facing X), filters hits to live IDamageables of the
    /// opposite faction by COMPONENT (no physics layers needed), sorts nearest-first and applies
    /// damage to up to maxTargets. Returns how many were hit (for combo / hit-pause feedback).
    /// </summary>
    public static class MeleeHitbox
    {
        private static readonly Collider[] s_Buffer = new Collider[32];
        private static readonly List<IDamageable> s_Hits = new List<IDamageable>(16);

        public static int ApplyMelee(
            Vector3 attackerPos,
            float facingSign,
            float reach,
            float damage,
            float knockback,
            HitType hitType,
            Faction attackerFaction,
            int maxTargets,
            Component sourceActor = null)
        {
            float halfReach = reach * 0.5f;
            Vector3 center = new Vector3(
                attackerPos.x + facingSign * (halfReach + 0.1f),
                attackerPos.y + 0.9f,
                GameConfig.PlayZ);
            Vector3 halfExtents = new Vector3(halfReach + 0.15f, GameConfig.HitboxHalfHeight, GameConfig.HitboxHalfWidth);

            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, s_Buffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

            s_Hits.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider col = s_Buffer[i];
                if (col == null) continue;
                IDamageable d = col.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive) continue;
                if (d.Faction == attackerFaction) continue;
                if (!s_Hits.Contains(d)) s_Hits.Add(d);
            }

            // Nearest first so cleave hits the closest enemies.
            s_Hits.Sort((a, b) =>
            {
                float da = Mathf.Abs(a.Transform.position.x - attackerPos.x);
                float db = Mathf.Abs(b.Transform.position.x - attackerPos.x);
                return da.CompareTo(db);
            });

            int applied = 0;
            for (int i = 0; i < s_Hits.Count && applied < maxTargets; i++)
            {
                var info = new DamageInfo(damage, attackerPos, knockback, hitType, attackerFaction, sourceActor);
                s_Hits[i].TakeDamage(in info);
                applied++;
            }
            return applied;
        }
    }
}
