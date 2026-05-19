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

            SetEnemyAction(enemyActions, enemyA, DefaultSkillAssetProvider.GetSlash());
            SetEnemyAction(enemyActions, enemyB, DefaultSkillAssetProvider.GetPierce());
            SetEnemyAction(enemyActions, enemyC, DefaultSkillAssetProvider.GetSlash());
            SetEnemyAction(enemyActions, enemyD, DefaultSkillAssetProvider.GetTwinHit());
            SetEnemyAction(enemyActions, enemyReserve, DefaultSkillAssetProvider.GetSlash());
        }

        public static EnemyActionData SelectEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy)
        {
            return GetEnemyAction(enemyActions, enemy);
        }

        private static void SetEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy,
            SkillData skill)
        {
            if (enemyActions == null || enemy == null || skill == null)
            {
                return;
            }

            enemyActions[enemy] = new EnemyActionData
            {
                Skill = skill
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
                Skill = DefaultSkillAssetProvider.GetSlash()
            };
        }
    }
}
