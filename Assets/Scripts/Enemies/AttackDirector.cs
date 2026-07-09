using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Crowd control for the endless siege: enemies may only wind up an attack while holding
    /// a token (Brutes cost 2), so a 14-enemy wave reads as a fair fight instead of a blender.
    /// Two windups can never start closer together than WindupStaggerGap. Tokenless enemies
    /// hold a shuffling ring just outside attack range (see EnemyBase). Token count escalates
    /// from wave 10 so pressure keeps rising.
    /// </summary>
    public static class AttackDirector
    {
        private struct Holder
        {
            public EnemyBase Enemy;
            public int Cost;
        }

        private static readonly List<Holder> s_Holders = new List<Holder>(4);
        private static float s_LastWindup = -99f;

        private static int MaxTokens
        {
            get
            {
                var gm = GameManager.Instance;
                int wave = gm != null && gm.Waves != null ? gm.Waves.CurrentWave : 1;
                return wave >= GameConfig.TokenEscalationWave
                    ? GameConfig.MaxAttackTokensLate
                    : GameConfig.MaxAttackTokens;
            }
        }

        /// <summary>Try to acquire attack permission. Idempotent for a current holder.</summary>
        public static bool RequestToken(EnemyBase enemy, int cost)
        {
            Prune();
            int used = 0;
            for (int i = 0; i < s_Holders.Count; i++)
            {
                if (s_Holders[i].Enemy == enemy) return true;
                used += s_Holders[i].Cost;
            }
            if (Time.time - s_LastWindup < GameConfig.WindupStaggerGap) return false;
            if (used + cost > MaxTokens) return false;

            s_Holders.Add(new Holder { Enemy = enemy, Cost = cost });
            s_LastWindup = Time.time;
            return true;
        }

        public static void ReleaseToken(EnemyBase enemy)
        {
            for (int i = s_Holders.Count - 1; i >= 0; i--)
            {
                if (s_Holders[i].Enemy == enemy) s_Holders.RemoveAt(i);
            }
        }

        /// <summary>Run restart / wipe: drop every token.</summary>
        public static void Clear()
        {
            s_Holders.Clear();
            s_LastWindup = -99f;
        }

        private static void Prune()
        {
            for (int i = s_Holders.Count - 1; i >= 0; i--)
            {
                if (s_Holders[i].Enemy == null || !s_Holders[i].Enemy.IsAlive) s_Holders.RemoveAt(i);
            }
        }
    }
}
