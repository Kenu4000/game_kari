using System.Collections.Generic;

namespace GameKari.Battle
{
    public enum EnemyTargetPattern
    {
        SameGridPosAlly,
        AllyFrontTop,
        AllyFrontBottom,
        BothFrontAllies,
        AllAllies
    }

    public class EnemyActionData
    {
        public string ActionName;
        public int Damage;
        public EnemyTargetPattern TargetPattern;
    }

    public static class DummyEnemyActionFactory
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

            SetEnemyAction(enemyActions, enemyA, "Claw", 60, EnemyTargetPattern.SameGridPosAlly);
            SetEnemyAction(enemyActions, enemyB, "Arrow", 45, EnemyTargetPattern.AllyFrontTop);
            SetEnemyAction(enemyActions, enemyC, "Bite", 60, EnemyTargetPattern.AllyFrontBottom);
            SetEnemyAction(enemyActions, enemyD, "Hex", 25, EnemyTargetPattern.AllAllies);
            SetEnemyAction(enemyActions, enemyReserve, "Strike", 60, EnemyTargetPattern.SameGridPosAlly);
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
            string actionName,
            int damage,
            EnemyTargetPattern targetPattern)
        {
            if (enemyActions == null || enemy == null)
            {
                return;
            }

            enemyActions[enemy] = new EnemyActionData
            {
                ActionName = actionName,
                Damage = damage,
                TargetPattern = targetPattern
            };
        }

        private static EnemyActionData GetEnemyAction(
            Dictionary<BattleUnit, EnemyActionData> enemyActions,
            BattleUnit enemy)
        {
            if (enemyActions != null &&
                enemy != null &&
                enemyActions.TryGetValue(enemy, out EnemyActionData action))
            {
                return action;
            }

            return CreateFallbackAction();
        }

        private static EnemyActionData CreateFallbackAction()
        {
            return new EnemyActionData
            {
                ActionName = "Strike",
                Damage = 60,
                TargetPattern = EnemyTargetPattern.SameGridPosAlly
            };
        }
    }
}



