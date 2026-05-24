using UnityEngine;

namespace GameKari.Battle
{
    internal static class QuestBattleCountUtility
    {
        public static int CountTotalBattleRoutePoints(QuestProgressState questProgress, int fallbackTotal)
        {
            if (questProgress == null || questProgress.Quest == null || questProgress.Quest.RoutePoints == null)
            {
                return Mathf.Max(1, fallbackTotal);
            }

            int count = 0;
            for (int i = 0; i < questProgress.Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = questProgress.Quest.RoutePoints[i];
                if (point != null && point.HasBattleData)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountClearedBattleRoutePoints(QuestProgressState questProgress, int fallbackCurrent)
        {
            if (questProgress == null || questProgress.Quest == null || questProgress.Quest.RoutePoints == null)
            {
                return Mathf.Max(1, fallbackCurrent);
            }

            int count = 0;
            for (int i = 0; i <= questProgress.CurrentRoutePointIndex && i < questProgress.Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = questProgress.Quest.RoutePoints[i];
                if (point != null && point.HasBattleData)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
