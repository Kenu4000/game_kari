using System.Collections.Generic;

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
            return GetEnemyActionState(enemyActionStates, enemy);
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

        private static EnemyActionState GetEnemyActionState(
            Dictionary<BattleUnit, EnemyActionState> enemyActionStates,
            BattleUnit enemy)
        {
            if (enemyActionStates != null &&
                enemy != null &&
                enemyActionStates.TryGetValue(enemy, out EnemyActionState actionState) &&
                actionState != null &&
                actionState.Skill != null)
            {
                return actionState;
            }

            return new EnemyActionState
            {
                Skill = SelectEnemySkill(enemy)
            };
        }

        private static SkillData SelectEnemySkill(BattleUnit enemy)
        {
            if (enemy != null && enemy.Skills != null)
            {
                for (int i = 0; i < enemy.Skills.Count; i++)
                {
                    SkillData skill = enemy.Skills[i];
                    if (skill != null)
                    {
                        return skill;
                    }
                }
            }

            return DefaultSkillAssetProvider.GetEnemyStrike();
        }
    }
}
