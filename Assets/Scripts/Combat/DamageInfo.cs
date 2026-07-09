using UnityEngine;

namespace Ironhold
{
    /// <summary>Which side an actor is on. Melee only hits the opposite faction.</summary>
    public enum Faction { Player, Enemy }

    /// <summary>
    /// Reaction class of a hit. Order matters: anything >= Heavy counts as a heavy hit for
    /// stagger / feedback rules. Launch pops light enemies airborne; Knockdown floors the victim.
    /// </summary>
    public enum HitType { Light, Heavy, Launch, Knockdown }

    /// <summary>A single melee hit, passed from an attacker's hitbox to an IDamageable.</summary>
    public struct DamageInfo
    {
        public float Amount;        // raw damage before block / scaling
        public Vector3 SourcePos;   // world position of the attacker (for facing / knockback dir)
        public float Knockback;     // metres of horizontal push along X
        public HitType Hit;         // reaction class (stagger / launch / knockdown rules)
        public Faction Attacker;    // who dealt it
        public Component SourceActor; // the attacking behaviour (parry needs to punish it); may be null

        public bool IsHeavy => Hit >= HitType.Heavy;

        public DamageInfo(float amount, Vector3 sourcePos, float knockback, HitType hit, Faction attacker,
            Component sourceActor = null)
        {
            Amount = amount;
            SourcePos = sourcePos;
            Knockback = knockback;
            Hit = hit;
            Attacker = attacker;
            SourceActor = sourceActor;
        }
    }
}
