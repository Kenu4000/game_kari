namespace GameKari.Battle
{
    /// <summary>
    /// 1クエスト内の進行状態を持つランタイム用クラス。
    /// 現在は旧Wave進行との互換を保ちながら、固定RoutePoint進行へ移行中。
    /// </summary>
    public sealed class QuestProgressState
    {
        public QuestData Quest { get; }
        public int CurrentWaveIndex { get; private set; }
        public int CurrentRoutePointIndex { get; private set; }

        public QuestProgressState(QuestData quest)
        {
            Quest = quest;
            CurrentWaveIndex = 0;
            CurrentRoutePointIndex = FindFirstBattleRoutePointIndex();
            ResolveCurrentWaveIndexFromRoute();
        }

        public RoutePointData CurrentRoutePoint
        {
            get
            {
                if (Quest == null || Quest.RoutePoints.Count == 0)
                {
                    return null;
                }

                if (CurrentRoutePointIndex < 0 || CurrentRoutePointIndex >= Quest.RoutePoints.Count)
                {
                    return null;
                }

                return Quest.RoutePoints[CurrentRoutePointIndex];
            }
        }

        public RoutePointData CurrentBattleRoutePoint
        {
            get
            {
                RoutePointData point = CurrentRoutePoint;
                if (point != null && point.HasBattleData)
                {
                    return point;
                }

                return null;
            }
        }

        public WaveData CurrentWave
        {
            get
            {
                if (Quest == null || Quest.Waves.Count == 0)
                {
                    return null;
                }

                if (CurrentWaveIndex < 0 || CurrentWaveIndex >= Quest.Waves.Count)
                {
                    return null;
                }

                return Quest.Waves[CurrentWaveIndex];
            }
        }

        public bool HasNextRoutePoint
        {
            get
            {
                return Quest != null
                    && CurrentRoutePointIndex >= 0
                    && CurrentRoutePointIndex + 1 < Quest.RoutePoints.Count;
            }
        }

        public bool HasNextWave
        {
            get
            {
                return HasNextBattleRoutePoint();
            }
        }

        public bool MoveNextRoutePoint()
        {
            if (!HasNextRoutePoint)
            {
                return false;
            }

            CurrentRoutePointIndex++;
            ResolveCurrentWaveIndexFromRoute();
            return true;
        }

        public bool MoveToNextBattleRoutePoint()
        {
            if (Quest == null || Quest.RoutePoints.Count == 0)
            {
                return MoveNextWaveFallback();
            }

            int nextIndex = CurrentRoutePointIndex + 1;
            while (nextIndex < Quest.RoutePoints.Count)
            {
                RoutePointData point = Quest.RoutePoints[nextIndex];
                if (point != null && point.HasBattleData)
                {
                    CurrentRoutePointIndex = nextIndex;
                    ResolveCurrentWaveIndexFromRoute();
                    return true;
                }

                nextIndex++;
            }

            return false;
        }

        public bool MoveNextWave()
        {
            return MoveToNextBattleRoutePoint();
        }

        private bool HasNextBattleRoutePoint()
        {
            if (Quest == null)
            {
                return false;
            }

            if (Quest.RoutePoints.Count == 0)
            {
                return CurrentWaveIndex + 1 < Quest.Waves.Count;
            }

            for (int i = CurrentRoutePointIndex + 1; i < Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = Quest.RoutePoints[i];
                if (point != null && point.HasBattleData)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveCurrentWaveIndexFromRoute()
        {
            RoutePointData point = CurrentBattleRoutePoint;
            if (point == null)
            {
                return;
            }

            if (point.WaveIndex < 0)
            {
                return;
            }

            CurrentWaveIndex = point.WaveIndex;
        }

        private int FindFirstBattleRoutePointIndex()
        {
            if (Quest == null || Quest.RoutePoints.Count == 0)
            {
                return 0;
            }

            for (int i = 0; i < Quest.RoutePoints.Count; i++)
            {
                RoutePointData point = Quest.RoutePoints[i];
                if (point != null && point.HasBattleData)
                {
                    return i;
                }
            }

            return 0;
        }

        private bool MoveNextWaveFallback()
        {
            if (Quest == null || CurrentWaveIndex + 1 >= Quest.Waves.Count)
            {
                return false;
            }

            CurrentWaveIndex++;
            return true;
        }
    }
}
