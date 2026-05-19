using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public class EnemyActionState
    {
        public SkillData Skill;
    }

    public static class EnemyActionSelector
    {
        public static void InitializeEnemyActionStates(
            Dictionary<BattleUnit, EnemyActionState> enemyActionStates,
            BattleUnit enemyA,
            BattleUnit enemyB,
            BattleUnit enemyC,
            BattleUnit enemyD,
            BattleUnit enemyReserve)
        {
            if (enemyActionStates == null)
            {
                return;
            }

            SetEnemyActionState(enemyActionStates, enemyA);
            SetEnemyActionState(enemyActionStates, enemyB);
            SetEnemyActionState(enemyActionStates, enemyC);
            SetEnemyActionState(enemyActionStates, enemyD);
            SetEnemyActionState(enemyActionStates, enemyReserve);
        }

        public static EnemyActionState ResolveEnemyActionState(
            Dictionary<BattleUnit, EnemyActionState> enemyActionStates,
            BattleUnit enemy)
        {
            EnemyActionState actionState = new EnemyActionState
            {
                Skill = SelectEnemySkill(enemy)
            };

            if (enemyActionStates != null && enemy != null)
            {
                enemyActionStates[enemy] = actionState;
            }

            return actionState;
        }

        private static void SetEnemyActionState(
            Dictionary<BattleUnit, EnemyActionState> enemyActionStates,
            BattleUnit enemy)
        {
            if (enemyActionStates == null || enemy == null)
            {
                return;
            }

            enemyActionStates[enemy] = new EnemyActionState
            {
                Skill = SelectEnemySkill(enemy)
            };
        }

        private static SkillData SelectEnemySkill(BattleUnit enemy)
        {
            SkillData weightedSkill = SelectWeightedEnemyActionSlotSkill(enemy);
            if (weightedSkill != null)
            {
                return weightedSkill;
            }

            SkillData firstRuntimeSkill = SelectFirstRuntimeSkill(enemy);
            if (firstRuntimeSkill != null)
            {
                return firstRuntimeSkill;
            }

            return DefaultSkillAssetProvider.GetEnemyStrike();
        }

        private static SkillData SelectWeightedEnemyActionSlotSkill(BattleUnit enemy)
        {
            if (enemy == null || enemy.Data == null || enemy.Data.EnemyActionSlots == null)
            {
                return null;
            }

            List<EnemyActionSlot> slots = enemy.Data.EnemyActionSlots;
            int totalWeight = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                EnemyActionSlot slot = slots[i];
                if (slot == null || slot.Skill == null || slot.Weight <= 0)
                {
                    continue;
                }

                totalWeight += slot.Weight;
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                EnemyActionSlot slot = slots[i];
                if (slot == null || slot.Skill == null || slot.Weight <= 0)
                {
                    continue;
                }

                cumulativeWeight += slot.Weight;
                if (roll < cumulativeWeight)
                {
                    return slot.Skill;
                }
            }

            return null;
        }

        private static SkillData SelectFirstRuntimeSkill(BattleUnit enemy)
        {
            if (enemy == null || enemy.Skills == null)
            {
                return null;
            }

            for (int i = 0; i < enemy.Skills.Count; i++)
            {
                SkillData skill = enemy.Skills[i];
                if (skill != null)
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
