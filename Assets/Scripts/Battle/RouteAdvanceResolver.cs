using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    internal enum RouteAdvanceDestinationType
    {
        QuestResult,
        Event,
        BattlePreparation
    }

    internal sealed class RouteAdvanceResult
    {
        public RouteAdvanceDestinationType DestinationType;
        public RoutePointData Point;
        public int StartRoutePointIndex;
        public int RouteCount;
        public bool HadNextRoutePoint;
        public readonly List<string> Logs = new();
    }

    /// <summary>
    /// Advances QuestProgressState to the next meaningful route destination.
    /// This class decides where the route should stop, but does not show UI.
    /// </summary>
    internal static class RouteAdvanceResolver
    {
        public static RouteAdvanceResult Advance(QuestProgressState questProgress)
        {
            RouteAdvanceResult result = new RouteAdvanceResult();

            if (questProgress == null)
            {
                result.DestinationType = RouteAdvanceDestinationType.QuestResult;
                result.Logs.Add("[Route] Advance requested, but quest progress is null. Showing Quest Result.");
                return result;
            }

            result.StartRoutePointIndex = questProgress.CurrentRoutePointIndex;
            result.RouteCount = questProgress.Quest == null || questProgress.Quest.RoutePoints == null
                ? 0
                : questProgress.Quest.RoutePoints.Count;
            result.HadNextRoutePoint = questProgress.HasNextRoutePoint;

            if (!questProgress.HasNextRoutePoint)
            {
                result.DestinationType = RouteAdvanceDestinationType.QuestResult;
                result.Logs.Add("[Route] No next route point. Showing Quest Result instead of returning to Base.");
                return result;
            }

            while (questProgress.MoveNextRoutePoint())
            {
                RoutePointData point = questProgress.CurrentRoutePoint;
                if (point == null)
                {
                    result.Logs.Add($"[Route] Passed null route point. CurrentIndex={questProgress.CurrentRoutePointIndex}.");
                    continue;
                }

                result.Logs.Add($"[Route] Arrived point: {point.DisplayName} ({point.PointType}), WaveIndex={point.WaveIndex}, HasBattleData={point.HasBattleData}.");

                if (point.PointType == RoutePointType.Normal || point.PointType == RoutePointType.Start)
                {
                    result.Logs.Add($"[Route] Passed point: {point.DisplayName} ({point.PointType}).");
                    continue;
                }

                result.Point = point;

                if (point.PointType == RoutePointType.Event)
                {
                    result.DestinationType = RouteAdvanceDestinationType.Event;
                    return result;
                }

                if (point.HasBattleData)
                {
                    result.DestinationType = RouteAdvanceDestinationType.BattlePreparation;
                    return result;
                }
            }

            result.DestinationType = RouteAdvanceDestinationType.QuestResult;
            result.Logs.Add("[Route] Route advance reached end. Showing Quest Result instead of returning to Base.");
            return result;
        }
    }
}
