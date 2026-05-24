using System.Collections.Generic;

namespace GameKari.Battle
{
    internal static class EnemyActionPreviewTextBuilder
    {
        public static string BuildPreviewText(
            List<BattleUnit> enemies,
            ISet<BattleUnit> actedUnits,
            BattleUnit nextEnemy,
            System.Func<BattleUnit, EnemyActionState> getActionState)
        {
            List<string> lines = new()
            {
                "Enemy Actions"
            };

            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    BattleUnit enemy = enemies[i];

                    if (enemy == null || enemy.IsDead || (actedUnits != null && actedUnits.Contains(enemy)))
                    {
                        continue;
                    }

                    EnemyActionState action = getActionState?.Invoke(enemy);
                    if (action == null || action.Skill == null)
                    {
                        continue;
                    }

                    lines.Add(BuildPreviewLine(enemy, action, enemy == nextEnemy));
                }
            }

            if (lines.Count == 1)
            {
                lines.Add("-");
            }

            return string.Join("\n", lines);
        }

        public static string BuildPreviewLine(BattleUnit enemy, EnemyActionState action, bool isNext)
        {
            if (enemy == null || action == null || action.Skill == null)
            {
                return "-";
            }

            string prefix = isNext ? "NEXT > " : string.Empty;
            return $"{prefix}{enemy.Name}: {action.Skill.SkillName} -> {BuildTargetText(enemy, action)}";
        }

        public static string BuildTargetText(BattleUnit enemy, EnemyActionState action)
        {
            if (action == null || action.Skill == null)
            {
                return "Unknown";
            }

            switch (action.Skill.TargetPattern)
            {
                case SkillTargetPattern.SameGridPosOpponent:
                    return enemy == null
                        ? "Ally same position"
                        : $"Ally {FormatGridPos(enemy.GridPos)}";

                case SkillTargetPattern.FrontTopOpponent:
                    return "Ally FrontTop";

                case SkillTargetPattern.FrontBottomOpponent:
                    return "Ally FrontBottom";

                case SkillTargetPattern.BothFrontOpponents:
                    return "Ally front row";

                case SkillTargetPattern.AllOpponents:
                    return "All allies";

                default:
                    return "Unknown";
            }
        }

        private static string FormatGridPos(GridPos pos)
        {
            switch (pos)
            {
                case GridPos.FrontTop:
                    return "FrontTop";

                case GridPos.BackTop:
                    return "BackTop";

                case GridPos.FrontBottom:
                    return "FrontBottom";

                case GridPos.BackBottom:
                    return "BackBottom";

                default:
                    return pos.ToString();
            }
        }
    }
}
