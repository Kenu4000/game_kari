using System.Collections.Generic;

namespace GameKari.Battle
{
    public class EnemyActionData
    {
        public SkillData Skill;
    }

    public static class DefaultEnemyActionProvider
    {
        public static void SetDefaultEnemyActions(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemyA,
            BattleUnit enemyB,
            BattleUnit enemyC,
            BattleUnit enemyD,
            BattleUnit enemyReserve)
        {
            if (enemyActions == null)
            {
                return;
            }

            SetEnemyAction(enemyActions, enemyA);
            SetEnemyAction(enemyActions, enemyB);
            SetEnemyAction(enemyActions, enemyC);
            SetEnemyAction(enemyActions, enemyD);
            SetEnemyAction(enemyActions, enemyReserve);
        }

        public static EnemyActionData SelectEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy)
        {
            return GetEnemyAction(enemyActions, enemy);
        }

        private static void SetEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy)
        {
            if (enemyActions == null || enemy == null)
            {
                return;
            }

            enemyActions[enemy] = new EnemyActionData
            {
                Skill = GetPrimaryEnemySkill(enemy)
            };
        }

        private static EnemyActionData GetEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy)
        {
            if (enemyActions != null &&
                enemy != null &&
                enemyActions.TryGetValue(enemy, out EnemyActionData action) &&
                action != null &&
                action.Skill != null)
            {
                return action;
            }

            return new EnemyActionData
            {
                Skill = GetPrimaryEnemySkill(enemy)
            };
        }

        private static SkillData GetPrimaryEnemySkill(BattleUnit enemy)
        {
            if (enemy != null && enemy.Skills != null && enemy.Skills.Count > 0 && enemy.Skills[0] != null)
            {
                return enemy.Skills[0];
            }

            return DefaultSkillAssetProvider.GetEnemyStrike();
        }
    }
}
