using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    internal static class BattlePartyStateUtility
    {
        public static int CountLivingMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        public static int CountKnownMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.Data == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        public static int HealLivingMembers(List<BattleUnit> units, int healAmount)
        {
            if (units == null || healAmount <= 0)
            {
                return 0;
            }

            int changedCount = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                int beforeHp = unit.CurrentHP;
                unit.CurrentHP = Mathf.Min(unit.Data.MaxHP, unit.CurrentHP + healAmount);

                if (unit.CurrentHP != beforeHp)
                {
                    changedCount++;
                    Debug.Log($"[Battle] {unit.Name} recovered HP {beforeHp}->{unit.CurrentHP}/{unit.Data.MaxHP}.");
                }
                else
                {
                    Debug.Log($"[Battle] {unit.Name} was eligible for HP reward but stayed at {unit.CurrentHP}/{unit.Data.MaxHP}.");
                }
            }

            return changedCount;
        }
    }
}
