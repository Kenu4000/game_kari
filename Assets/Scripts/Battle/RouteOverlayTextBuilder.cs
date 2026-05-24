using UnityEngine;

namespace GameKari.Battle
{
    /// <summary>
    /// Builds the temporary route overlay text shown between battles.
    /// This class has no UI references and does not advance route state.
    /// </summary>
    internal static class RouteOverlayTextBuilder
    {
        public static string BuildRouteMovementText(QuestProgressState questProgress)
        {
            return
                $"{GetQuestRouteTitleText(questProgress)}\n\n" +
                $"Current\n{GetCurrentRoutePointText(questProgress)}\n\n" +
                $"Route\n{BuildRouteBarText(questProgress)}\n\n" +
                $"{GetNextImportantRoutePointText(questProgress)}\n\n" +
                "Action\n" +
                "Button: Move";
        }

        public static string BuildRouteEventText(RoutePointData point)
        {
            string displayName = GetRoutePointDisplayName(point, "Route Event");
            string eventText = point == null || string.IsNullOrEmpty(point.EventText)
                ? "An event occurs on the route."
                : point.EventText;

            return
                $"{displayName}\n\n" +
                $"{eventText}\n\n" +
                "After Event\n" +
                "Button: Next → Movement";
        }

        public static string BuildBattlePreparationText(
            RoutePointData point,
            string partyOverviewText,
            int kakeraStock,
            int maxKakeraStock,
            bool isScouted,
            WaveData wave)
        {
            string displayName = GetRoutePointDisplayName(point, "Battle Point");

            return
                $"{displayName}\n\n" +
                $"Party\n{partyOverviewText}\n\n" +
                $"Kakera\n{kakeraStock}/{maxKakeraStock}\n\n" +
                $"Enemy Info\n{BuildEnemyScoutStateText(point, isScouted, wave)}\n\n" +
                $"{BuildPreparationActionHintText(point, kakeraStock, isScouted)}\n\n" +
                "Action\n" +
                "Button: Start Battle";
        }

        private static string GetQuestRouteTitleText(QuestProgressState questProgress)
        {
            if (questProgress == null || questProgress.Quest == null)
            {
                return "Quest Route";
            }

            return string.IsNullOrEmpty(questProgress.Quest.QuestName)
                ? "Quest Route"
                : questProgress.Quest.QuestName;
        }

        private static string GetCurrentRoutePointText(QuestProgressState questProgress)
        {
            if (questProgress == null || questProgress.CurrentRoutePoint == null)
            {
                return "Unknown";
            }

            RoutePointData current = questProgress.CurrentRoutePoint;
            string displayName = GetRoutePointDisplayName(current, "Unknown");
            int currentIndex = Mathf.Max(0, questProgress.CurrentRoutePointIndex);
            int totalCount = questProgress.Quest == null || questProgress.Quest.RoutePoints == null
                ? 0
                : questProgress.Quest.RoutePoints.Count;

            return totalCount <= 0
                ? $"{displayName} ({current.PointType})"
                : $"{displayName} ({current.PointType}) / {currentIndex + 1} of {totalCount}";
        }

        private static string BuildRouteBarText(QuestProgressState questProgress)
        {
            if (questProgress == null || questProgress.Quest == null || questProgress.Quest.RoutePoints.Count == 0)
            {
                return "Route: unavailable";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < questProgress.Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = questProgress.Quest.RoutePoints[i];
                if (i > 0)
                {
                    builder.Append(" -> ");
                }

                if (i == questProgress.CurrentRoutePointIndex)
                {
                    builder.Append("[Truck]");
                    continue;
                }

                builder.Append(GetRoutePointSymbol(point));
            }

            return builder.ToString();
        }

        private static string GetNextImportantRoutePointText(QuestProgressState questProgress)
        {
            if (questProgress == null || questProgress.Quest == null)
            {
                return "Next Point: unknown";
            }

            for (int i = questProgress.CurrentRoutePointIndex + 1; i < questProgress.Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = questProgress.Quest.RoutePoints[i];
                if (point == null)
                {
                    continue;
                }

                if (point.PointType == RoutePointType.Normal || point.PointType == RoutePointType.Start)
                {
                    continue;
                }

                string displayName = GetRoutePointDisplayName(point, point.PointType.ToString());
                int segmentCount = Mathf.Max(1, i - questProgress.CurrentRoutePointIndex);

                return
                    "Next Point\n" +
                    $"{displayName} ({point.PointType})\n" +
                    $"Segments: {segmentCount}";
            }

            return "Next Point\nBase Return";
        }

        private static string BuildPreparationActionHintText(RoutePointData point, int kakeraStock, bool isScouted)
        {
            if (point == null || !point.HasBattleData)
            {
                return "Scout: unavailable";
            }

            if (isScouted)
            {
                return "Scout: already completed";
            }

            if (kakeraStock <= 0)
            {
                return "Scout: requires Kakera 1";
            }

            return "Scout: available for Kakera 1";
        }

        private static string BuildEnemyScoutStateText(RoutePointData point, bool isScouted, WaveData wave)
        {
            if (point == null || !point.HasBattleData)
            {
                return "Unavailable";
            }

            if (!isScouted)
            {
                return "Unscouted\nDetails hidden.";
            }

            if (wave == null)
            {
                return "Scouted\nEnemies: unknown\nRoles: deferred";
            }

            int activeCount = wave.EnemyPlacements == null ? 0 : wave.EnemyPlacements.Count;
            int reserveCount = wave.EnemyReserves == null ? 0 : wave.EnemyReserves.Count;

            return
                "Scouted\n" +
                $"Enemies: {activeCount} active / {reserveCount} reserve\n" +
                $"Formation:\n{BuildEnemyPlacementSummary(wave)}\n" +
                "Roles: deferred";
        }

        private static string BuildEnemyPlacementSummary(WaveData wave)
        {
            if (wave == null || wave.EnemyPlacements == null || wave.EnemyPlacements.Count == 0)
            {
                return "none";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < wave.EnemyPlacements.Count; i++)
            {
                BattleUnitPlacement placement = wave.EnemyPlacements[i];
                if (placement == null)
                {
                    continue;
                }

                string unitName = placement.Unit == null
                    ? "Unknown"
                    : placement.Unit.Name;

                builder.Append("- ");
                builder.Append(placement.Position);
                builder.Append(": ");
                builder.Append(unitName);

                if (i < wave.EnemyPlacements.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.Length == 0 ? "none" : builder.ToString();
        }

        private static string GetRoutePointSymbol(RoutePointData point)
        {
            if (point == null)
            {
                return "?";
            }

            return point.PointType switch
            {
                RoutePointType.Start => "Start",
                RoutePointType.Normal => "·",
                RoutePointType.Battle => "Battle",
                RoutePointType.Event => "Event",
                RoutePointType.Boss => "Boss",
                _ => "?"
            };
        }

        private static string GetRoutePointDisplayName(RoutePointData point, string fallback)
        {
            if (point == null)
            {
                return fallback;
            }

            return string.IsNullOrEmpty(point.DisplayName)
                ? fallback
                : point.DisplayName;
        }
    }
}
